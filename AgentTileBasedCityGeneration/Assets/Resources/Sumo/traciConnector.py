import traci
import time
import traci.constants as tc
import socket
import os

from SumoInfoUtility import VehicleInfo
from SumoInfoUtility import SumoSimulationStepInfo

# documentation at https://sumo.dlr.de/pydoc/traci.html

IS_DEBUG = False

time_step_seconds = 1.0 / 30.0

should_stop = False


### Connection Methods ###

def start_socket_server():
    if IS_DEBUG:
        print("Starting socket server...")
        return None, None
    # Setup socket server
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.bind(('localhost', 9999))
    server_socket.listen(1)

    print("Waiting for a connection from Unity...")
    conn, addr = server_socket.accept()
    print("Connected by", addr)

    return conn, addr


def send_data_over_socket(data, conn):
    print("Sending data:")
    if IS_DEBUG:
        return
    conn.sendall(data.encode('utf-8'))


def receive_data_over_socket(conn):
    if IS_DEBUG:
        return
    data = conn.recv(1024)
    print(f"Received data: {data}")
    if data.decode('utf-8') == "stop":
        should_stop = True


### Additional TraCI Methods ###

def get_vehicle_info(id):
    pos = traci.vehicle.getPosition(id)
    rot = traci.vehicle.getAngle(id)
    rot = rot + 180
    speed = traci.vehicle.getSpeed(id)
    signals = traci.vehicle.getSignals(id)
    veh_type = traci.vehicle.getVehicleClass(id)

    vehicle = VehicleInfo(id, pos, rot, speed, signals, veh_type)
    # print(vehicle)
    return vehicle


def add_vehicle(vehicle_id, route_id):
    traci.vehicle.add(vehicle_id, route_id)
    traci.vehicle.setSpeedMode(vehicle_id, 0)  # Disable automatic speed regulation


def move_vehicle(vehicle_id, x, y):
    traci.vehicle.moveToXY(vehicle_id, "", 0, x, y, angle=0, keepRoute=2)


### Main Loop ###

def run_sumo_simulation():
    # Setup SUMO
    sumoBinary = "sumo"
    sumoCmd = [sumoBinary, "-c", "configuration.sumocfg", "--ignore-route-errors"]
    traci.start(sumoCmd)

    #    # Connect to SUMO with TraCI
    #    traci.connect(port=57230)

    # Setup socket server
    conn, addr = start_socket_server()

    # Prepare Simulation loop
    step = 0
    start_time = time.time()
    next_frame_time = start_time
    # Main Simulation Loop
    while traci.simulation.getMinExpectedNumber() > 0 and not should_stop:
        current_time = time.time()
        
        # check if it is time to advance the simulation
        if current_time < next_frame_time:
            continue

        next_frame_time += time_step_seconds
        
        # advance the simulation by one step
        traci.simulationStep()
        step += 1
        print(f"Step {step}:")

        if current_time > next_frame_time:
            print(f"WARNING: Simulation is running too slow! Skipping frame {step}...")
            continue

        vehicle_list = list()

        id_list = traci.vehicle.getIDList()
        # print(f"Vehicle IDs: {id_list}")
        for i in range(0,len(id_list)):
            vehicle_id = id_list[i]
            vehicle = get_vehicle_info(vehicle_id)
            vehicle_list.append(vehicle)
            # print(vehicle)

        simulation_step_info = SumoSimulationStepInfo(step, vehicle_list)
        simulation_step_info_json = simulation_step_info.convert_to_json_string()

        send_data_over_socket(simulation_step_info_json, conn)
        receive_data_over_socket(conn)

        # sleep_time = time_step_seconds - (time.time() - current_time)
        # if sleep_time > 0:
        #     print(f"Sleeping for {sleep_time} seconds...")
        #     time.sleep(sleep_time)
        # time.sleep(1.0/30.0)


    # Close TraCI connection
    traci.close()
    conn.close()


if __name__ == "__main__":
    cwd = os.getcwd()
    print(cwd)
    run_sumo_simulation()
