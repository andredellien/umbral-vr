import { CatalogService } from './catalog.service';

describe('CatalogService', () => {
  const catalog = new CatalogService().get();

  it('trae al menos una unidad con área y precio base positivos', () => {
    expect(catalog.units.length).toBeGreaterThan(0);
    for (const u of catalog.units) {
      expect(u.areaM2).toBeGreaterThan(0);
      expect(u.basePrice).toBeGreaterThan(0);
    }
  });

  it('la primera opción de cada superficie está incluida en el precio base (delta 0)', () => {
    expect(catalog.surfaces.length).toBeGreaterThan(0);
    for (const s of catalog.surfaces) {
      expect(s.options.length).toBeGreaterThan(0);
      expect(s.options[0].delta).toBe(0);
    }
  });

  it('los ids son únicos', () => {
    const ids = [
      ...catalog.units.map((u) => u.id),
      ...catalog.surfaces.map((s) => s.id),
      ...catalog.surfaces.flatMap((s) => s.options.map((o) => o.id)),
    ];
    expect(new Set(ids).size).toBe(ids.length);
  });

  it('tiene fecha de vigencia', () => {
    expect(catalog.validUntil).toMatch(/^\d{4}-\d{2}-\d{2}$/);
  });
});
