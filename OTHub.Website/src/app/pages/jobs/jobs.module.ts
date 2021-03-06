import { OffersComponent } from './offers/offers.component';
import { NgModule } from '@angular/core';
import {CommonModule, DatePipe} from '@angular/common';
import { ThemeModule } from '../../@theme/theme.module';
import { JobsRoutingModule } from './jobs-routing.module';
import { MomentModule } from 'ngx-moment';
import { OffersDetailComponent } from './offersdetail/offersdetail.component';
import { SharedModule } from '../shared.module';
import { NbCardModule, NbIconModule, NbPopoverModule, NbSelectModule, NbButtonModule, NbStepperModule, NbListModule, NbBadgeModule } from '@nebular/theme';
import { Ng2SmartTableModule } from 'ng2-smart-table';
import { RouterModule } from '@angular/router';
import {DataCreatorColumnComponent} from './offers/datacreatorcolumn.component';

@NgModule({
  declarations: [ OffersComponent, OffersDetailComponent, DataCreatorColumnComponent],
  imports: [
    CommonModule,
    JobsRoutingModule,
    MomentModule,
    SharedModule,
    ThemeModule,
    NbCardModule,
    NbIconModule,
    Ng2SmartTableModule,
    NbSelectModule,
    NbButtonModule,
    NbStepperModule,
    NbPopoverModule,
    NbListModule,
    NbBadgeModule,
    RouterModule,
  ],
  providers: [
    DatePipe
  ]
})
export class JobsModule { }
