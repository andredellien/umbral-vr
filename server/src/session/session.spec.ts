import { INestApplication } from '@nestjs/common';
import { WsAdapter } from '@nestjs/platform-ws';
import { Test } from '@nestjs/testing';
import { AddressInfo } from 'net';
import { WebSocket } from 'ws';
import { AppModule } from '../app.module';

interface Msg { event: string; data?: any }

class Client {
  readonly msgs: Msg[] = [];
  private constructor(readonly ws: WebSocket) {
    ws.on('message', (m) => this.msgs.push(JSON.parse(m.toString())));
  }
  static async connect(url: string) {
    const ws = new WebSocket(url);
    await new Promise((r, j) => { ws.once('open', r); ws.once('error', j); });
    return new Client(ws);
  }
  send(event: string, data?: unknown) { this.ws.send(JSON.stringify({ event, data })); }
  async next(event: string): Promise<Msg> {
    for (let i = 0; i < 100; i++) {
      const idx = this.msgs.findIndex((m) => m.event === event);
      if (idx >= 0) return this.msgs.splice(idx, 1)[0];
      await new Promise((r) => setTimeout(r, 20));
    }
    throw new Error(`No llegó '${event}'; recibido: ${JSON.stringify(this.msgs)}`);
  }
  async quiet(event: string) {
    await new Promise((r) => setTimeout(r, 150));
    expect(this.msgs.find((m) => m.event === event)).toBeUndefined();
  }
  close() { this.ws.close(); }
}

describe('Sala de sesión (WebSocket)', () => {
  let app: INestApplication;
  let url: string;
  const open: Client[] = [];
  const connect = async () => { const c = await Client.connect(url); open.push(c); return c; };

  beforeAll(async () => {
    const mod = await Test.createTestingModule({ imports: [AppModule] }).compile();
    app = mod.createNestApplication();
    app.useWebSocketAdapter(new WsAdapter(app));
    await app.listen(0);
    url = `ws://127.0.0.1:${(app.getHttpServer().address() as AddressInfo).port}/session`;
  });
  afterAll(async () => { open.forEach((c) => c.close()); await app.close(); });

  it('retransmite el estado del visor al asesor y los comandos del asesor al visor', async () => {
    const host = await connect(), adv = await connect();
    host.send('host', { code: '1111' });
    expect((await host.next('hosted')).data.code).toBe('1111');
    adv.send('join', { code: '1111' });
    await adv.next('joined');
    await host.next('advisor-joined');

    host.send('state', { stage: 1, total: 520000 });
    expect((await adv.next('state')).data).toEqual({ stage: 1, total: 520000 });
    adv.send('command', { cmd: 'pause' });
    expect((await host.next('command')).data).toEqual({ cmd: 'pause' });
  });

  it('el asesor que llega tarde recibe de inmediato el último estado', async () => {
    const host = await connect();
    host.send('host', { code: '2222' });
    await host.next('hosted');
    host.send('state', { stage: 2 });
    host.send('state', { stage: 3 });
    await new Promise((r) => setTimeout(r, 100));
    const adv = await connect();
    adv.send('join', { code: '2222' });
    expect((await adv.next('state')).data).toEqual({ stage: 3 });
  });

  it('rechaza el rol equivocado', async () => {
    const host = await connect(), adv = await connect();
    host.send('host', { code: '3333' });
    await host.next('hosted');
    adv.send('join', { code: '3333' });
    await adv.next('joined');
    adv.send('state', { stage: 9 });
    expect((await adv.next('error')).data.message).toMatch(/visor/);
    host.send('command', { cmd: 'pause' });
    expect((await host.next('error')).data.message).toMatch(/asesor/);
    await adv.quiet('state');
  });

  it('rechaza código desconocido, código en uso y código mal formado', async () => {
    const a = await connect(), b = await connect();
    a.send('join', { code: '9999' });
    expect((await a.next('error')).data.message).toMatch(/desconocido/);
    a.send('host', { code: '4444' });
    await a.next('hosted');
    b.send('host', { code: '4444' });
    expect((await b.next('error')).data.message).toMatch(/en uso/);
    b.send('host', { code: 'abc' });
    expect((await b.next('error')).data.message).toMatch(/inválido/);
  });

  it('borra la sala cuando se va el visor y avisa a los asesores', async () => {
    const host = await connect(), adv = await connect();
    host.send('host', { code: '5555' });
    await host.next('hosted');
    adv.send('join', { code: '5555' });
    await adv.next('joined');
    host.close();
    await adv.next('host-left');
    const again = await connect();
    again.send('join', { code: '5555' });
    expect((await again.next('error')).data.message).toMatch(/desconocido/);
  });

  it('avisa al visor cuando se va el asesor y el código vuelve a servir al reconectar', async () => {
    const host = await connect(), adv = await connect();
    host.send('host', { code: '6666' });
    await host.next('hosted');
    adv.send('join', { code: '6666' });
    await adv.next('joined');
    await host.next('advisor-joined');
    adv.close();
    await host.next('advisor-left');

    host.close();
    await new Promise((r) => setTimeout(r, 150));
    const host2 = await connect();
    host2.send('host', { code: '6666' });
    await host2.next('hosted');
  });
});
