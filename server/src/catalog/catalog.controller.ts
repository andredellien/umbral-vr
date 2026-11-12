import { Controller, Get } from '@nestjs/common';
import { ReservationsService } from '../reservations/reservations.service';
import { Catalog, CatalogService } from './catalog.service';

@Controller()
export class CatalogController {
  constructor(
    private readonly catalog: CatalogService,
    private readonly reservations: ReservationsService,
  ) {}

  @Get('health')
  health() {
    return { ok: true };
  }

  @Get('catalog')
  getCatalog(): Catalog {
    return this.catalog.withReservations(this.reservations.reservedUnitIds());
  }
}
