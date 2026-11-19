import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ParkingAvailability, ParkedVehicle, ParkingExit, ParkingService } from './parking.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.component.html'
})
export class AppComponent implements OnInit {
  private readonly parking = inject(ParkingService);
  availability: ParkingAvailability | null = null;
  vehicleReg = '';
  vehicleType = 1;
  exitReg = '';
  parked: ParkedVehicle | null = null;
  receipt: ParkingExit | null = null;
  error = '';
  loading = false;

  ngOnInit(): void { this.refresh(); }

  refresh(): void {
    this.parking.availability().subscribe({
      next: data => this.availability = data,
      error: err => this.error = this.message(err)
    });
  }

  park(): void {
    if (!this.vehicleReg.trim() || this.loading) return;
    this.error = '';
    this.parked = null;
    this.receipt = null;
    this.loading = true;
    this.parking.park(this.vehicleReg.trim(), Number(this.vehicleType)).subscribe({
      next: result => {
        this.parked = result;
        this.vehicleReg = '';
        this.loading = false;
        this.refresh();
      },
      error: err => { this.error = this.message(err); this.loading = false; }
    });
  }

  exit(): void {
    if (!this.exitReg.trim() || this.loading) return;
    this.error = '';
    this.receipt = null;
    this.parked = null;
    this.loading = true;
    this.parking.exit(this.exitReg.trim()).subscribe({
      next: result => {
        this.receipt = result;
        this.exitReg = '';
        this.loading = false;
        this.refresh();
      },
      error: err => { this.error = this.message(err); this.loading = false; }
    });
  }

  private message(err: HttpErrorResponse): string {
    return err.error?.Error ?? 'Could not connect to the parking API. Confirm the backend is running on port 5080.';
  }
}
