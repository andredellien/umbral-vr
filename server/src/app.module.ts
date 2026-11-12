import { Module } from '@nestjs/common';
import { CatalogController } from './catalog/catalog.controller';
import { CatalogService } from './catalog/catalog.service';
import { RoomsService } from './session/rooms.service';
import { SessionGateway } from './session/session.gateway';

@Module({
  controllers: [CatalogController],
  providers: [CatalogService, RoomsService, SessionGateway],
})
export class AppModule {}
