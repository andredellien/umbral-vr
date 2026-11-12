import { INestApplication } from '@nestjs/common';
import { WsAdapter } from '@nestjs/platform-ws';
import { Test } from '@nestjs/testing';
import { mkdtempSync, readFileSync, rmSync } from 'fs';
import { tmpdir } from 'os';
import { AddressInfo } from 'net';
import { join } from 'path';
import { AppModule } from '../app.module';
import { CatalogService } from '../catalog/catalog.service';

describe('Reservas y telemetría (HTTP)', () => {
  let app: INestApplication;
  let dir: string;
  let base: string;
  const seed = new CatalogService().get();
  const unit = seed.units.find((u) => (u.status ?? 'available') === 'available')!;

  async function start(): Promise<void> {
    process.env.UMBRAL_DATA_DIR = dir;
    const mod = await Test.createTestingModule({ imports: [AppModule] }).compile();
    app = mod.createNestApplication();
    app.useWebSocketAdapter(new WsAdapter(app));
    await app.listen(0);
    base = `http://127.0.0.1:${(app.getHttpServer().address() as AddressInfo).port}`;
  }

  beforeEach(async () => {
    dir = mkdtempSync(join(tmpdir(), 'umbral-'));
    await start();
  });
  afterEach(async () => {
    await app.close();
    rmSync(dir, { recursive: true, force: true });
    delete process.env.UMBRAL_DATA_DIR;
  });

  // Cliente HTTP mínimo sobre fetch: .post(...).send(...).expect(...) como supertest, sin dependencia extra.
  const http = () => ({
    post: (path: string) => ({
      send: (body: unknown) => {
        const p = fetch(base + path, { method: 'POST', headers: { 'content-type': 'application/json' }, body: JSON.stringify(body) })
          .then(async (r) => ({ status: r.status, body: await r.json().catch(() => null) as any }));
        return Object.assign(p, {
          expect: async (status: number) => { const r = await p; expect(r.status).toBe(status); return r; },
        });
      },
    }),
    get: (path: string) => {
      const p = fetch(base + path).then(async (r) => ({ status: r.status, body: await r.json().catch(() => null) as any }));
      return Object.assign(p, { expect: async (status: number) => { const r = await p; expect(r.status).toBe(status); return r; } });
    },
  });

  it('el total lo recalcula el servidor y no confía en un precio del cliente', async () => {
    const floor = seed.surfaces.find((s) => s.id === 'floor')!.options[2];
    const res = await http()
      .post('/reservations')
      .send({ unitId: unit.id, total: 1, selections: [{ surface: 'floor', option: floor.id }] })
      .expect(201);
    expect(res.body.total).toBe(unit.basePrice + floor.delta);
    expect(res.body.selections).toHaveLength(seed.surfaces.length); // completa con las opciones incluidas
  });

  it('segunda reserva de la misma unidad: 409', async () => {
    await http().post('/reservations').send({ unitId: unit.id, selections: [] }).expect(201);
    await http().post('/reservations').send({ unitId: unit.id, selections: [] }).expect(409);
  });

  it('dos pedidos simultáneos: solo uno gana', async () => {
    const [a, b] = await Promise.all([
      http().post('/reservations').send({ unitId: unit.id, selections: [] }),
      http().post('/reservations').send({ unitId: unit.id, selections: [] }),
    ]);
    expect([a.status, b.status].sort()).toEqual([201, 409]);
  });

  it('una unidad vendida o reservada en el seed: 409; desconocida: 404; selección inválida: 400', async () => {
    const sold = seed.units.find((u) => u.status === 'sold')!;
    await http().post('/reservations').send({ unitId: sold.id, selections: [] }).expect(409);
    await http().post('/reservations').send({ unitId: 'Z-9', selections: [] }).expect(404);
    await http().post('/reservations').send({ unitId: unit.id, selections: [{ surface: 'floor', option: 'no-existe' }] }).expect(400);
    await http().post('/reservations').send({ unitId: unit.id, selections: [{ surface: 'nope', option: 'x' }] }).expect(400);
    await http().post('/reservations').send({ unitId: unit.id, selections: [{ surface: 'floor', option: 'floor-wood' }, { surface: 'floor', option: 'floor-ceramic' }] }).expect(400);
    await http().post('/reservations').send({ selections: [] }).expect(400);
  });

  it('GET /catalog marca la unidad reservada y el seed en disco no se modifica', async () => {
    const before = readFileSync(join(__dirname, '..', '..', 'seed', 'catalog.json'), 'utf8');
    await http().post('/reservations').send({ unitId: unit.id, selections: [] }).expect(201);
    const res = await http().get('/catalog').expect(200);
    expect(res.body.units.find((u: any) => u.id === unit.id).status).toBe('reserved');
    expect(readFileSync(join(__dirname, '..', '..', 'seed', 'catalog.json'), 'utf8')).toBe(before);
  });

  it('las reservas sobreviven a un reinicio del servidor', async () => {
    await http().post('/reservations').send({ unitId: unit.id, selections: [] }).expect(201);
    await app.close();
    await start();
    await http().post('/reservations').send({ unitId: unit.id, selections: [] }).expect(409);
  });

  it('eventos: reintentar el mismo lote no cuenta dos veces', async () => {
    const batch = { events: [{ id: 'e1', type: 'session_start' }, { id: 'e2', type: 'stage_entered', stage: 'Maqueta' }] };
    expect((await http().post('/events').send(batch).expect(200)).body).toEqual({ accepted: 2, duplicates: 0, rejected: 0 });
    expect((await http().post('/events').send(batch).expect(200)).body).toEqual({ accepted: 0, duplicates: 2, rejected: 0 });
    const lines = readFileSync(join(dir, 'events.jsonl'), 'utf8').trim().split('\n');
    expect(lines).toHaveLength(2);
  });

  it('eventos: malformados se rechazan sin tumbar el lote', async () => {
    const res = await http().post('/events').send({ events: [{ id: 'a', type: 'x' }, { type: 'sin-id' }, 5] }).expect(200);
    expect(res.body).toEqual({ accepted: 1, duplicates: 0, rejected: 2 });
    await http().post('/events').send({}).expect(400);
  });
});
