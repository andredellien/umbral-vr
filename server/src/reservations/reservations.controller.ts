import { Body, Controller, HttpCode, Post } from '@nestjs/common';
import { ReservationRequest, ReservationsService } from './reservations.service';

@Controller('reservations')
export class ReservationsController {
  constructor(private readonly reservations: ReservationsService) {}

  @Post()
  @HttpCode(201)
  create(@Body() body: ReservationRequest) {
    return this.reservations.create(body);
  }
}
