import numpy as np
import sys
from PIL import Image, ImageDraw
from queue import Queue
import heapq as hq
import colorsys
import matplotlib.pyplot as plt
from skimage.draw import line
import math
import random

grid_block_size = 11

# coordinates are based on "discretized" grid, not on actual pixels represented by heightmap,
def grid_coords_to_pixel_coords(x, grid_block_size): 
    x = np.array(x) # now it also works with tuples...
    return x * grid_block_size + grid_block_size // 2

def pixel_coords_to_grid_coords(x, grid_block_size):
    x = np.array(x) # now it also works with tuples...
    return x // grid_block_size

def cost_function(map_, prev_x, prev_y, x, y):
    grad_approx = (map_[y, x]-map_[prev_y, prev_x])/3
    grad_approx = (grad_approx * 0.1) ** 2
    return grad_approx

def shortest_path_astar(map_, start, end, mask):
    #h = lambda start, end: (abs(start[0] - end[0]) + abs(start[1] - end[1]))

    # euclidean distance as heuristic
    h = lambda start, end: math.sqrt((start[0]-end[0])**2 + (start[1]-end[1])**2)

    map_shape = (map_.shape[0], map_.shape[1])

    came_from = t = [ [(None, None)]*map_shape[1] for i in range(map_shape[0])]

    g_score = np.full(map_shape, np.inf)
    g_score[start[1], start[0]] = 0
    f_score = np.full(map_shape, np.inf)
    f_score[start[1], start[0]] = h(start, end)

    q = []
    hq.heappush(q, (0, start))

    iters = 0
    costs = []
   
    while len(q) != 0:
        prio , current = hq.heappop(q)
        x, y = current
        if np.array_equal(current, end):
            print("found")
            break

        for dx, dy in mask:
            neighbor_x = x + dx
            neighbor_y = y + dy


            if (neighbor_x < 0 or neighbor_x >= map_shape[1] or  # out of bounds
                neighbor_y < 0 or neighbor_y >= map_shape[0]): # out of bounds
                continue

            assert not np.isinf(g_score[y,x])
            cost = cost_function(map_, x,y,neighbor_x,neighbor_y)
            costs.append(cost)
            tentative_g_score = g_score[y,x] + 1 + cost
            if tentative_g_score < g_score[neighbor_y, neighbor_x]:
                came_from[neighbor_y][neighbor_x] = (x,y)
                g_score[neighbor_y, neighbor_x] = tentative_g_score
                f = tentative_g_score + h((neighbor_x, neighbor_y), end)
                f_score[neighbor_y, neighbor_x] = f
                hq.heappush(q, (f, (neighbor_x, neighbor_y)))

        iters += 1

    print(f"{iters=}")
    costs = np.array(costs)
    print(f"{costs.mean()=} {costs.min()=} {costs.max()=}")

    return came_from
            
def colorize_mpl(heightmap):
    cmap = plt.get_cmap("autumn_r")
    normalized_heightmap = (heightmap - heightmap.min()) / (heightmap.max() - heightmap.min())
    colorized_heightmap = cmap(normalized_heightmap)
    return (colorized_heightmap[:, :, :3] * 255).astype(np.uint8)

def draw_path(rgb_image, came_from, start, end):
    x, y = end
    x = pixel_coords_to_grid_coords(x, grid_block_size)
    y = pixel_coords_to_grid_coords(y, grid_block_size)
    prev_x, prev_y = end
    prev_x = pixel_coords_to_grid_coords(prev_x, grid_block_size)
    prev_y = pixel_coords_to_grid_coords(prev_y, grid_block_size)

    start = (pixel_coords_to_grid_coords(start[0], grid_block_size), pixel_coords_to_grid_coords(start[1], grid_block_size))

    rgb_image[y, x] = (255,0,0)
    while (x,y) != start:
        x, y, = came_from[y][x]
        pixel_x = grid_coords_to_pixel_coords(x, grid_block_size)
        pixel_y = grid_coords_to_pixel_coords(y, grid_block_size)
        pixel_prev_x = grid_coords_to_pixel_coords(prev_x, grid_block_size)
        pixel_prev_y = grid_coords_to_pixel_coords(prev_y, grid_block_size)

        rr, cc = line(pixel_prev_y, pixel_prev_x, pixel_y, pixel_x)
        rgb_image[rr, cc] = (0,0,255)
        prev_x, prev_y = x, y

    return rgb_image

def draw_grid(rgb_image, grid_block_size):
    for y in range(0, rgb_image.shape[1], grid_block_size):
        for x in range(0, rgb_image.shape[0], grid_block_size):
            rgb_image[y,x] = [0,0,0]

def get_discretized_grid(heightmap, grid_block_size):
    grid_dim_h = heightmap.shape[0] // grid_block_size
    grid_dim_w = heightmap.shape[1] // grid_block_size

    grid = np.zeros(shape=(grid_dim_h, grid_dim_w))

    for y in range(0, grid_dim_h):
        for x in range(0, grid_dim_w):
            pixel_x = grid_coords_to_pixel_coords(x, grid_block_size)
            pixel_y = grid_coords_to_pixel_coords(y, grid_block_size)

            grid[y,x] = heightmap[pixel_y, pixel_x]

    return grid

def multipath(heightmap, routes, mask):
    colorized_map = colorize_mpl(heightmap)
    for r in routes:
        start, end = r
        start_grid = pixel_coords_to_grid_coords(start, grid_block_size)
        end_grid = pixel_coords_to_grid_coords(end, grid_block_size)

        grid = get_discretized_grid(heightmap, grid_block_size)

        assert start_grid[1] < grid.shape[1] and start_grid[0] < grid.shape[0], "Startpoint outside of acceptable area"
        assert end_grid[1] < grid.shape[1] and end_grid[0] < grid.shape[0], "Endpoint outside of acceptable area"

        came_from = shortest_path_astar(grid, start_grid, end_grid, mask)
        colorized_map = draw_path(colorized_map, came_from, start, end)

    draw_grid(colorized_map, grid_block_size)
    return colorized_map

def get_random_paths(heightmap, num_points, grid_block_size):
    max_w = heightmap.shape[0] - (heightmap.shape[0] % grid_block_size)
    max_h = heightmap.shape[1] - (heightmap.shape[1] % grid_block_size)

    points = []
    for i in range(num_points):
        start_x = random.randrange(max_w)
        start_y = random.randrange(max_h)
        points.append((start_x, start_y))

    paths = []
        
    while len(points) != 0:
        point_1 = points.pop()
        for point_2 in points:
            if point_1 == point_2:
                continue
        
            paths.append((point_1, point_2))

    return paths


mask_4_neighbors = [(-1,0), (1,0), (0, -1), (0,1)]
mask_8_neighbors = [(-1,0), (1,0), (0, -1), (0,1), (-1, -1), (1,1), (-1,1), (1,-1)]
mask_16_neighbors = [(-1,0), (1,0), (0, -1), (0,1), (-1, -1), (1,1), (-1,1), (1,-1), (-2, -1), (-1, -2), (-2, 1), (-1,2), (2,-1), (1,-2),(2,1), (1,2)]

I = np.asarray(Image.open(sys.argv[1]))
print(I.shape)

random.seed(13371337)

# block size usually does not fit image perfectly. make sure the points don't lie in the cut off region
points = [
    ((755, 655), (200, 500)),
    ((177, 1033), (200, 490)),
    ((200, 490), (575, 847)),
    ((190, 190), (999,475)),
    ((177, 1033), (930,787)),
]

points = get_random_paths(I, 6, grid_block_size)

im = Image.fromarray(multipath(I, points, mask_8_neighbors))
im.save(sys.argv[2])