import traci
import time
import traci.constants as tc
import socket
import os
 
# Function to add a vehicle to the simulation
def add_vehicle(vehicle_id, route_id):
    traci.vehicle.add(vehicle_id, route_id)
    traci.vehicle.setSpeedMode(vehicle_id, 0)  # Disable automatic speed regulation
 
# Function to manually control the vehicle's steering
def move_vehicle(vehicle_id, x, y):
    traci.vehicle.moveToXY(vehicle_id, "", 0, x, y, angle=0, keepRoute=2)
    
 
def run_sumo_simulation():
    # Setup SUMO
    sumoBinary = "sumo"
    sumoCmd = [sumoBinary, "-c", "configuration.sumocfg"]
    traci.start(sumoCmd)

#    # Connect to SUMO with TraCI
#    traci.connect(port=57230)
    
    # Setup socket server
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.bind(('localhost', 9999))
    server_socket.listen(1)
    
    print("Waiting for a connection from Unity...")
    conn, addr = server_socket.accept()
    print("Connected by", addr)
     
    vehicle_id = "vehicle_0"
    route_id = "1_traffic"  # Ensure this route exists in your SUMO config
    add_vehicle(vehicle_id, route_id)
     
    # Simulation loop
    step = 0
    x = 5
    y = 2
    while traci.simulation.getMinExpectedNumber() > 0:
        traci.simulationStep()
     
#        x += 1
#        y += 1
#        move_vehicle(vehicle_id, x, y)

#        vehicle_ids = traci.vehicle.getIDList()
#        print(f"Step {step}:")
#        for vehicle_id in vehicle_ids:
#            position = traci.vehicle.getPosition(vehicle_id)
#            print(f"Vehicle {vehicle_id}: Position {position}")

        speed = traci.vehicle.getSpeed(vehicle_id)
        position = traci.vehicle.getPosition(vehicle_id)
        conn.sendall(f"{vehicle_id};{speed};{position}".encode('utf-8'))
        
        data = conn.recv(1024)
        if data.decode('utf-8') == "stop":
            break
     
        # Your simulation logic here
        step += 1
        #time.sleep(1)
        
     
    # Close TraCI connection
    traci.close()
    conn.close()

    
if __name__ == "__main__":
    cwd = os.getcwd()
    print(cwd)
    run_sumo_simulation()

