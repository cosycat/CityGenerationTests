import traci
import time
import traci.constants as tc
import socket
import os

from SumoInfoUtility import VehicleInfo
from SumoInfoUtility import SumoSimulationStepInfo

# documentation at https://sumo.dlr.de/pydoc/traci.html

IS_DEBUG = False
JSON_DIRECTLY = False
SKIP_IF_SLOW = True

TCP_IP = 'localhost'
TCP_PORT = 9999
BUFFER_SIZE = 64

time_step_seconds = 0.03

should_stop = False

### Signals ###

# a list of signals and their corresponding methods as lambda functions
signalsList = {
    "end_simulation": lambda: end_simulation(),
    "continue" : lambda: continue_simulation(),
}

def end_simulation():
    print("Ending simulation...")
    global should_stop
    should_stop = True

def continue_simulation():
    print("Continuing simulation...")
    pass

### Connection Methods ###

def start_socket_server():
    if IS_DEBUG:
        print("Starting socket server...")
        return None, None
    # Setup socket server
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.bind((TCP_IP, TCP_PORT))
    server_socket.listen(1)

    print("Waiting for a connection from Unity...")
    conn, addr = server_socket.accept()
    print("Connected by", addr)

    return conn, addr

def send_data_over_socket(data, conn):
    print(f"Sending data...\n")
    # print(data)
    if IS_DEBUG:
        return
    conn.sendall(data.encode('utf-8'))


def receive_data_over_socket(conn):
    if IS_DEBUG:
        return
    data = conn.recv(BUFFER_SIZE)
    print(f"Received data: {data}")
    # split data at newline character
    dataArray = data.split(b'\n')
    for signal, method in signalsList.items():
        if signal.encode('utf-8') in dataArray:
            method()
            break
        # if data.decode('utf-8') == signal:
        #     method()
        #     break
    print("other signal received...")
    # if data.decode('utf-8') == end_simulation_signal:
    #     should_stop = True


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

def get_vehicle_json_directly(id):
    string = "{"
    string += f"\"id\": \"{id}\","
    string += f"\"positionX\": {traci.vehicle.getPosition(id)[0]},"
    string += f"\"positionY\": {traci.vehicle.getPosition(id)[1]},"
    string += f"\"rotation\": {traci.vehicle.getAngle(id) + 180},"
    string += f"\"signals\": {traci.vehicle.getSignals(id)},"
    string += f"\"speed\": {traci.vehicle.getSpeed(id)},"
    string += f"\"vehicleType\": \"{traci.vehicle.getVehicleClass(id)}\""
    string += "}"

    return string

def get_vehicle_info_subscription(id, subscription_results):
    pos = (subscription_results[tc.VAR_POSITION][0], subscription_results[tc.VAR_POSITION][1])
    rot = subscription_results[tc.VAR_ANGLE]
    rot = rot + 180
    speed = subscription_results[tc.VAR_SPEED]
    signals = subscription_results[tc.VAR_SIGNALS]
    veh_type = subscription_results[tc.VAR_TYPE]

    vehicle = VehicleInfo(id, pos, rot, speed, signals, veh_type)
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
        traci.simulationStep() # TODO skip with parameter t here instead.
        step += 1
        print(f"Step {step}:")
        print(f"Time: {current_time - start_time}")
        print(f"Time to next frame: {next_frame_time - current_time}")
        print(f"Simulation time: {traci.simulation.getTime()}")
        # print(f"result:\n{resultStep}")

        # subscribe to all newly added vehicles
        for vehicle_id in traci.simulation.getDepartedIDList():
            traci.vehicle.subscribe(vehicle_id, (tc.VAR_POSITION, tc.VAR_ANGLE, tc.VAR_SPEED, tc.VAR_SIGNALS, tc.VAR_TYPE))

        # check if the simulation is running too slow
        if current_time > next_frame_time:
            if SKIP_IF_SLOW:
                print(f"WARNING: Simulation is running too slow! Skipping frame {step}...")
                continue
            else:
                print(f"WARNING: Simulation is running too slow!")
                pass


        id_list = traci.vehicle.getIDList()
        print(f"Vehicles: {len(id_list)}")
        # for i in range(0,len(id_list)):
        #     vehicle_id = id_list[i]
        #     print(traci.vehicle.getSubscriptionResults(vehicle_id))

        if JSON_DIRECTLY:
            vehicle_string = "["
            for i in range(0,len(id_list)):
                vehicle_id = id_list[i]
                vehicle_string += get_vehicle_json_directly(vehicle_id)
                if i < len(id_list) - 1:
                    vehicle_string += ","
            vehicle_string += "]"

            data_string = f"{{\"step\": {step}, \"vehicleList\": {vehicle_string}}}"

            send_data_over_socket(data_string, conn)

        else :
            vehicle_list = list()
            for i in range(0,len(id_list)):
                vehicle_id = id_list[i]
                subscription_result = traci.vehicle.getSubscriptionResults(vehicle_id)
                if subscription_result is None or len(subscription_result) == 0:
                    print(f"WARNING: No subscription result for vehicle {vehicle_id}: {subscription_result}")
                    continue
                vehicle = get_vehicle_info_subscription(vehicle_id, subscription_result)
                vehicle_list.append(vehicle)
                # print(vehicle)

            simulation_step_info = SumoSimulationStepInfo(step, vehicle_list)
            # print(simulation_step_info)
            # print(vehicle_list)
            simulation_step_info_json = simulation_step_info.convert_to_json_string()

            send_data_over_socket(simulation_step_info_json, conn)
        
        # receive_data_over_socket(conn)

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
