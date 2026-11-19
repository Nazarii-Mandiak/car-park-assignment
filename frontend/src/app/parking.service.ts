import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export interface ParkingAvailability { AvailableSpaces: number; OccupiedSpaces: number; }
export interface ParkedVehicle { VehicleReg: string; SpaceNumber: number; TimeIn: string; }
export interface ParkingExit { VehicleReg: string; VehicleCharge: number; TimeIn: string; TimeOut: string; }

@Injectable({ providedIn: 'root' })
export class ParkingService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5080';

  availability() { return this.http.get<ParkingAvailability>(`${this.baseUrl}/parking`); }
  park(VehicleReg: string, VehicleType: number) {
    return this.http.post<ParkedVehicle>(`${this.baseUrl}/parking`, { VehicleReg, VehicleType });
  }
  exit(VehicleReg: string) {
    return this.http.post<ParkingExit>(`${this.baseUrl}/parking/exit`, { VehicleReg });
  }
}
