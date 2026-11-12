import { ConnectedSocket, MessageBody, OnGatewayDisconnect, SubscribeMessage, WebSocketGateway } from '@nestjs/websockets';
import { WebSocket } from 'ws';
import { Peer, RoomError, RoomsService } from './rooms.service';

/** Mensajes: {"event": "...", "data": {...}}. Cliente -> host/join/state/command. Servidor -> hosted/joined/state/command/advisor-joined/advisor-left/host-left/error. */
@WebSocketGateway({ path: '/session' })
export class SessionGateway implements OnGatewayDisconnect {
  private readonly peers = new WeakMap<WebSocket, Peer>();

  constructor(private readonly rooms: RoomsService) {}

  private peer(client: WebSocket): Peer {
    let p = this.peers.get(client);
    if (!p) {
      p = {
        send: (event, data) => {
          if (client.readyState === WebSocket.OPEN) client.send(JSON.stringify({ event, data }));
        },
      };
      this.peers.set(client, p);
    }
    return p;
  }

  private run(client: WebSocket, ok: string, action: (p: Peer) => void, okData?: unknown) {
    const p = this.peer(client);
    try {
      action(p);
      if (ok) p.send(ok, okData);
    } catch (e) {
      if (e instanceof RoomError) p.send('error', { message: e.message });
      else throw e;
    }
  }

  @SubscribeMessage('host')
  host(@ConnectedSocket() client: WebSocket, @MessageBody() body: { code?: string }) {
    this.run(client, 'hosted', (p) => this.rooms.host(p, body?.code), { code: body?.code });
  }

  @SubscribeMessage('join')
  join(@ConnectedSocket() client: WebSocket, @MessageBody() body: { code?: string }) {
    this.run(client, 'joined', (p) => this.rooms.join(p, body?.code), { code: body?.code });
  }

  @SubscribeMessage('state')
  state(@ConnectedSocket() client: WebSocket, @MessageBody() body: unknown) {
    this.run(client, '', (p) => this.rooms.publishState(p, body));
  }

  @SubscribeMessage('command')
  command(@ConnectedSocket() client: WebSocket, @MessageBody() body: unknown) {
    this.run(client, '', (p) => this.rooms.sendCommand(p, body));
  }

  handleDisconnect(client: WebSocket) {
    const p = this.peers.get(client);
    if (p) this.rooms.leave(p);
  }
}
