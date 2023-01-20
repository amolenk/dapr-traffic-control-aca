import { Utils } from "./utils.js";

export class NoneTrafficControlService {
    
    constructor(baseUrl) {
        this.baseUrl = baseUrl;
    }

    registerVehicleEntry(car) {
        console.log(`entry cam: ${car.id}`);
    }

    registerVehicleExit(car) {
        console.log(`exit cam: ${car.id}`);
    }
}