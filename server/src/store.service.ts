import { Injectable } from '@nestjs/common';
import { appendFileSync, existsSync, mkdirSync, readFileSync, renameSync, writeFileSync } from 'fs';
import { join } from 'path';

export interface Reservation {
  id: string;
  unitId: string;
  selections: { surface: string; option: string }[];
  total: number; // Bs enteros, calculado aquí
  createdAt: string;
}

export interface StoredEvent {
  id: string;
  type: string;
  sessionId?: string;
  at?: string;
  [k: string]: unknown;
}

/**
 * Almacenamiento en archivos (reservas en un JSON, eventos en JSONL). No había PostgreSQL ni Docker en la máquina de
 * desarrollo (ADR #12); el contrato de este servicio es lo que se migraría a una base de datos.
 * La carpeta sale de UMBRAL_DATA_DIR (las pruebas usan una temporal) o de server/data.
 */
@Injectable()
export class StoreService {
  private readonly dir = process.env.UMBRAL_DATA_DIR ?? join(__dirname, '..', 'data');
  private readonly reservationsFile = join(this.dir, 'reservations.json');
  private readonly eventsFile = join(this.dir, 'events.jsonl');
  private reservations: Reservation[] = [];
  private readonly eventIds = new Set<string>();

  constructor() {
    mkdirSync(this.dir, { recursive: true });
    if (existsSync(this.reservationsFile)) this.reservations = JSON.parse(readFileSync(this.reservationsFile, 'utf8'));
    if (existsSync(this.eventsFile)) {
      for (const line of readFileSync(this.eventsFile, 'utf8').split('\n')) {
        if (!line.trim()) continue;
        try { this.eventIds.add(JSON.parse(line).id); } catch { /* línea a medio escribir: se ignora */ }
      }
    }
  }

  allReservations(): Reservation[] {
    return this.reservations;
  }

  addReservation(r: Reservation): void {
    this.reservations = [...this.reservations, r];
    const tmp = this.reservationsFile + '.tmp';
    writeFileSync(tmp, JSON.stringify(this.reservations, null, 2));
    renameSync(tmp, this.reservationsFile); // atómico: un corte no deja el archivo a medias
  }

  /** Guarda el evento si su id no se vio antes. Devuelve false si era un duplicado (reintento del cliente). */
  addEvent(e: StoredEvent): boolean {
    if (this.eventIds.has(e.id)) return false;
    appendFileSync(this.eventsFile, JSON.stringify(e) + '\n');
    this.eventIds.add(e.id);
    return true;
  }
}
