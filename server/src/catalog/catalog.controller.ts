import { Controller, Get } from '@nestjs/common';
import { Catalog, CatalogService } from './catalog.service';

@Controller()
export class CatalogController {
  constructor(private readonly catalog: CatalogService) {}

  @Get('health')
  health() {
    return { ok: true };
  }

  @Get('catalog')
  getCatalog(): Catalog {
    return this.catalog.get();
  }
}
