import { BadRequestException, Body, Controller, HttpCode, Post } from '@nestjs/common';
import { StoreService, StoredEvent } from '../store.service';

const MAX_BATCH = 500;

/** Telemetría del visor. El cliente reintenta lotes enteros: el id de cada evento es la clave de deduplicación. */
@Controller('events')
export class EventsController {
  constructor(private readonly store: StoreService) {}

  @Post()
  @HttpCode(200)
  ingest(@Body() body: { events?: unknown }) {
    if (!body || !Array.isArray(body.events)) throw new BadRequestException('Falta la lista "events"');
    if (body.events.length > MAX_BATCH) throw new BadRequestException(`Máximo ${MAX_BATCH} eventos por lote`);
    let accepted = 0, duplicates = 0, rejected = 0;
    for (const raw of body.events) {
      const e = raw as StoredEvent;
      if (!e || typeof e.id !== 'string' || !e.id || typeof e.type !== 'string' || !e.type) { rejected++; continue; }
      if (this.store.addEvent(e)) accepted++; else duplicates++;
    }
    return { accepted, duplicates, rejected };
  }
}
