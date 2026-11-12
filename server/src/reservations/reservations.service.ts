import { BadRequestException, ConflictException, Injectable, NotFoundException } from '@nestjs/common';
import { randomUUID } from 'crypto';
import { CatalogService } from '../catalog/catalog.service';
import { Reservation, StoreService } from '../store.service';

export interface ReservationRequest {
  unitId?: unknown;
  selections?: unknown;
}

@Injectable()
export class ReservationsService {
  constructor(
    private readonly catalog: CatalogService,
    private readonly store: StoreService,
  ) {}

  reservedUnitIds(): Set<string> {
    return new Set(this.store.allReservations().map((r) => r.unitId));
  }

  /**
   * Reserva una unidad. El total lo calcula el servidor (base + diferencias de los acabados, sin descuentos):
   * nunca se confía en el precio del cliente. Es síncrono y el proceso es único, así que dos visores que piden la
   * misma unidad no pueden ganar los dos: el segundo recibe 409.
   */
  create(req: ReservationRequest): Reservation {
    const catalog = this.catalog.get();
    if (typeof req?.unitId !== 'string') throw new BadRequestException('Falta unitId');
    const unit = catalog.units.find((u) => u.id === req.unitId);
    if (!unit) throw new NotFoundException(`Unidad desconocida '${req.unitId}'`);

    const chosen = new Map<string, string>();
    const selections = req.selections ?? [];
    if (!Array.isArray(selections)) throw new BadRequestException('selections debe ser una lista');
    for (const s of selections) {
      if (typeof s?.surface !== 'string' || typeof s?.option !== 'string') throw new BadRequestException('Cada selección lleva surface y option');
      if (chosen.has(s.surface)) throw new BadRequestException(`Superficie repetida '${s.surface}'`);
      const surface = catalog.surfaces.find((x) => x.id === s.surface);
      if (!surface) throw new BadRequestException(`Superficie desconocida '${s.surface}'`);
      if (!surface.options.some((o) => o.id === s.option)) throw new BadRequestException(`Opción desconocida '${s.option}' en '${s.surface}'`);
      chosen.set(s.surface, s.option);
    }

    if ((unit.status ?? 'available') !== 'available' || this.reservedUnitIds().has(unit.id)) {
      throw new ConflictException(`La unidad ${unit.id} no está disponible`);
    }

    let total = unit.basePrice;
    const full: { surface: string; option: string }[] = [];
    for (const surface of catalog.surfaces) {
      const optionId = chosen.get(surface.id) ?? surface.options[0].id; // sin elección = la incluida (delta 0)
      total += surface.options.find((o) => o.id === optionId)!.delta;
      full.push({ surface: surface.id, option: optionId });
    }

    const reservation: Reservation = { id: randomUUID(), unitId: unit.id, selections: full, total, createdAt: new Date().toISOString() };
    this.store.addReservation(reservation);
    return reservation;
  }
}
