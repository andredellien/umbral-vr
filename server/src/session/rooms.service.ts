import { Injectable } from '@nestjs/common';

export type Role = 'host' | 'advisor';

export interface Peer {
  send(event: string, data?: unknown): void;
}

interface Room {
  host: Peer;
  advisors: Set<Peer>;
  state?: unknown;
}

export class RoomError extends Error {}

/** Salas en memoria: el visor es el anfitrión, el asesor se une con el código. */
@Injectable()
export class RoomsService {
  private readonly rooms = new Map<string, Room>();
  private readonly byPeer = new Map<Peer, { code: string; role: Role }>();

  host(peer: Peer, code: unknown) {
    if (typeof code !== 'string' || !/^\d{4}$/.test(code)) throw new RoomError('Código inválido');
    if (this.byPeer.has(peer)) throw new RoomError('Ya estás en una sala');
    if (this.rooms.has(code)) throw new RoomError('Código en uso');
    this.rooms.set(code, { host: peer, advisors: new Set() });
    this.byPeer.set(peer, { code, role: 'host' });
  }

  join(peer: Peer, code: unknown) {
    const room = typeof code === 'string' ? this.rooms.get(code) : undefined;
    if (!room) throw new RoomError('Código desconocido');
    if (this.byPeer.has(peer)) throw new RoomError('Ya estás en una sala');
    room.advisors.add(peer);
    this.byPeer.set(peer, { code: code as string, role: 'advisor' });
    room.host.send('advisor-joined');
    if (room.state !== undefined) peer.send('state', room.state);
  }

  /** Solo el anfitrión publica estado; se guarda para quien llegue tarde. */
  publishState(peer: Peer, state: unknown) {
    const { room } = this.require(peer, 'host');
    room.state = state;
    room.advisors.forEach((a) => a.send('state', state));
  }

  /** Solo el asesor manda comandos; van al visor. */
  sendCommand(peer: Peer, command: unknown) {
    const { room } = this.require(peer, 'advisor');
    room.host.send('command', command);
  }

  leave(peer: Peer) {
    const info = this.byPeer.get(peer);
    if (!info) return;
    this.byPeer.delete(peer);
    const room = this.rooms.get(info.code);
    if (!room) return;
    if (info.role === 'host') {
      this.rooms.delete(info.code);
      room.advisors.forEach((a) => {
        this.byPeer.delete(a);
        a.send('host-left');
      });
    } else {
      room.advisors.delete(peer);
      room.host.send('advisor-left');
    }
  }

  private require(peer: Peer, role: Role) {
    const info = this.byPeer.get(peer);
    if (!info) throw new RoomError('No estás en una sala');
    if (info.role !== role) throw new RoomError(`Solo el ${role === 'host' ? 'visor' : 'asesor'} puede hacer esto`);
    return { room: this.rooms.get(info.code)!, info };
  }
}
