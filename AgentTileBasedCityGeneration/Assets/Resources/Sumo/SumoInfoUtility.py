import json


# Inspired by https://github.com/TUM-VT/Sumonity/


class SumoSimulationStepInfo:
    time = 0
    vehicleList = list()

    def __init__(self, _time, _vehicle_list):
        self.time = _time
        self.vehicles = _vehicle_list

    def convert_to_json_string(self):
        return self.convert_vehicle_list_to_json_string(self.vehicles)

    @staticmethod
    def convert_vehicle_list_to_json_string(vehicle_list):
        vehicle_dict_list = list()
        for vehicle in vehicle_list:
            vehicle_dict = {
                "id": vehicle.id,
                "positionX": vehicle.positionX,
                "positionY": vehicle.positionY,
                "rotation": vehicle.rotation,
                # "speed": vehicle.speed,
                # "signals": vehicle.signals,
                # "vehicleType": vehicle.vehicleType,
            }
            vehicle_dict_list.append(vehicle_dict)
        return json.dumps(vehicle_dict_list)


class VehicleInfo:
    id = ""
    positionX = 0
    positionY = 0
    rotation = 0
    speed = 0
    signals = None
    vehicleType = ""
    lookaheadPosX = 0
    lookaheadPosY = 0

    def __init__(self, _id, _pos, _rot, _speed, _signals, _veh_type):
        self.id = _id
        self.positionX = _pos[0]
        self.positionY = _pos[1]
        self.rotation = _rot
        self.speed = _speed
        self.signals = _signals
        self.vehicleType = _veh_type
