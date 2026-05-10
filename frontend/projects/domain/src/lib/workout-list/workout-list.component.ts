import { Component, Inject, OnInit, computed, signal } from '@angular/core';
import { Router } from '@angular/router';
import {
  EQUIPMENT_SERVICE,
  EquipmentItem,
  IEquipmentService,
  ISessionsService,
  SESSIONS_SERVICE,
  Session,
  SessionListQuery,
  SessionRange
} from 'api';
import {
  ButtonComponent,
  CardComponent,
  ChipComponent,
  EmptyStateComponent
} from 'components';
import { MatChipsModule } from '@angular/material/chips';

interface RangeChip {
  id: SessionRange;
  label: string;
}

const RANGE_CHIPS: RangeChip[] = [
  { id: 'all', label: 'All' },
  { id: 'today', label: 'Today' },
  { id: 'week', label: 'This week' },
  { id: 'month', label: 'This month' }
];

@Component({
  selector: 'forge-workout-list',
  imports: [
    CardComponent,
    ChipComponent,
    EmptyStateComponent,
    ButtonComponent,
    MatChipsModule
  ],
  templateUrl: './workout-list.component.html',
  styleUrl: './workout-list.component.scss'
})
export class WorkoutListComponent implements OnInit {
  protected readonly equipment = signal<EquipmentItem[]>([]);
  protected readonly selectedEquipment = signal<EquipmentItem['id'] | null>(null);
  protected readonly selectedRange = signal<SessionRange>('all');
  protected readonly sessions = signal<Session[]>([]);
  protected readonly loading = signal(false);

  protected readonly rangeChips = RANGE_CHIPS;

  protected readonly hasSessions = computed(() => this.sessions().length > 0);

  constructor(
    @Inject(SESSIONS_SERVICE) private readonly sessionsApi: ISessionsService,
    @Inject(EQUIPMENT_SERVICE) private readonly equipmentApi: IEquipmentService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.equipmentApi.list().subscribe({
      next: (items) => this.equipment.set(items),
      error: () => undefined
    });
    this.refresh();
  }

  protected selectEquipment(id: EquipmentItem['id'] | null): void {
    this.selectedEquipment.set(id);
    this.refresh();
  }

  protected selectRange(range: SessionRange): void {
    this.selectedRange.set(range);
    this.refresh();
  }

  protected openSession(id: string): void {
    this.router.navigate(['/workouts', id]);
  }

  protected newSession(): void {
    this.router.navigate(['/workouts', 'new']);
  }

  private refresh(): void {
    const query: SessionListQuery = {
      range: this.selectedRange(),
      page: 1,
      pageSize: 50
    };
    const equipment = this.selectedEquipment();
    if (equipment) {
      query.equipment = equipment;
    }
    this.loading.set(true);
    this.sessionsApi.list(query).subscribe({
      next: (page) => {
        this.sessions.set(page.items);
        this.loading.set(false);
      },
      error: () => {
        this.sessions.set([]);
        this.loading.set(false);
      }
    });
  }
}
