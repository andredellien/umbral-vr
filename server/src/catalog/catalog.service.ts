import { Injectable } from '@nestjs/common';
import { readFileSync } from 'fs';
import { join } from 'path';

export interface CatalogOption {
  id: string;
  name: string;
  delta: number; // Bs de diferencia frente a la opción incluida (la primera tiene delta 0)
}

export interface CatalogSurface {
  id: string;
  name: string;
  options: CatalogOption[];
}

export interface CatalogUnit {
  id: string;
  floor: number;
  areaM2: number;
  basePrice: number; // Bs enteros
}

export interface Catalog {
  version: number;
  validUntil: string; // yyyy-MM-dd, se muestra en el visor
  project: { id: string; name: string };
  units: CatalogUnit[];
  surfaces: CatalogSurface[];
}

/**
 * Semana 1: el catálogo sale de un JSON versionado en el repo.
 * Si más adelante hace falta editarlo sin redeploy, se mueve a PostgreSQL.
 */
@Injectable()
export class CatalogService {
  private readonly path = join(__dirname, '..', '..', 'seed', 'catalog.json');

  get(): Catalog {
    return JSON.parse(readFileSync(this.path, 'utf8')) as Catalog;
  }
}
