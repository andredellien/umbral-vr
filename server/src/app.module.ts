import { Module } from '@nestjs/common';
import { CatalogController } from './catalog/catalog.controller';
import { CatalogService } from './catalog/catalog.service';
import { EventsController } from './events/events.controller';
import { ReservationsController } from './reservations/reservations.controller';
import { ReservationsService } from './reservations/reservations.service';
import { RoomsService } from './session/rooms.service';
import { SessionGateway } from './session/session.gateway';
import { StoreService } from './store.service';

@Module({
  controllers: [CatalogController, ReservationsController, EventsController],
  providers: [CatalogService, ReservationsService, StoreService, RoomsService, SessionGateway],
})
export class AppModule {}
