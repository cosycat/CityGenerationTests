import traci
import time
import traci.constants as tc
import socket
import os

# documentation at https://sumo.dlr.de/pydoc/traci.html

IS_DEBUG = False

time_step_seconds = 1.0/30.0

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
    print(f"Sending data: {data}")
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

    # Simulation loop
    step = 0
    while traci.simulation.getMinExpectedNumber() > 0 and not should_stop:
        current_time = time.time()
        
        traci.simulationStep()
        print(f"Step {step}:")

        positions = []
        for vehicle_id in traci.vehicle.getIDList():
            position = traci.vehicle.getPosition(vehicle_id)
            positions.append((vehicle_id, position))

        send_data_over_socket(str(positions), conn)
        receive_data_over_socket(conn)

        step += 1
        
        sleep_time = time_step_seconds - (time.time() - current_time)
        if sleep_time > 0:
            time.sleep(sleep_time)
        # time.sleep(1.0/30.0)

    # Close TraCI connection
    traci.close()
    conn.close()


if __name__ == "__main__":
    cwd = os.getcwd()
    print(cwd)
    run_sumo_simulation()
