import { CommonModule } from '@angular/common';
import { Component, DestroyRef, Inject, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
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

interface DayGroup {
  key: string;
  label: string;
  sessions: Session[];
}

const RANGE_CHIPS: RangeChip[] = [
  { id: 'all', label: 'All' },
  { id: 'today', label: 'Today' },
  { id: 'week', label: 'This week' },
  { id: 'month', label: 'This month' }
];

const EQUIPMENT_ICON: Record<string, string> = {
  Treadmill: 'directions_run',
  IndoorBike: 'directions_bike',
  BenchPress: 'fitness_center',
  Elliptical: 'fitness_center'
};

const EQUIPMENT_TINT: Record<string, string> = {
  Treadmill: 'workout-list__row-icon--green',
  IndoorBike: 'workout-list__row-icon--amber',
  BenchPress: 'workout-list__row-icon--orange',
  Elliptical: 'workout-list__row-icon--blue'
};

const EQUIPMENT_LABEL: Record<string, string> = {
  Treadmill: 'Treadmill',
  IndoorBike: 'Indoor bike',
  BenchPress: 'Bench press',
  Elliptical: 'Elliptical'
};

@Component({
  selector: 'forge-workout-list',
  imports: [
    CommonModule,
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

  private readonly destroyRef = inject(DestroyRef);

  protected readonly rangeChips = RANGE_CHIPS;

  protected readonly hasSessions = computed(() => this.sessions().length > 0);

  protected readonly summaryMinutes = computed(() =>
    this.sessions().reduce((sum, s) => sum + (s.durationMinutes ?? 0), 0)
  );
  protected readonly summaryCalories = computed(() =>
    this.sessions().reduce((sum, s) => sum + (s.activeCalories ?? 0), 0)
  );
  protected readonly summaryPoints = computed(() =>
    // Approximate: 2 points per logged minute (matches the Points Breakdown
    // base rate on /workouts/new). Real backend points may differ.
    this.summaryMinutes() * 2
  );

  protected readonly subtitle = computed(() => {
    const n = this.sessions().length;
    const min = this.summaryMinutes();
    const hours = Math.floor(min / 60);
    const rest = min % 60;
    const dur = hours > 0 ? `${hours} h ${rest} min` : `${rest} min`;
    return `${n} session${n === 1 ? '' : 's'} · ${dur} · ${this.summaryCalories()} cal`;
  });

  protected readonly dayGroups = computed<DayGroup[]>(() => {
    const groups = new Map<string, DayGroup>();
    const today = startOfDay(new Date());
    const yesterday = startOfDay(new Date(Date.now() - 86_400_000));
    for (const s of this.sessions()) {
      const d = startOfDay(new Date(s.startedAt));
      const key = d.toISOString();
      let label: string;
      if (d.getTime() === today.getTime()) {
        label = 'Today';
      } else if (d.getTime() === yesterday.getTime()) {
        label = 'Yesterday';
      } else {
        label = d.toLocaleDateString(undefined, {
          weekday: 'long',
          month: 'short',
          day: 'numeric'
        });
      }
      if (!groups.has(key)) {
        groups.set(key, { key, label, sessions: [] });
      }
      groups.get(key)!.sessions.push(s);
    }
    return Array.from(groups.values()).sort((a, b) => (a.key < b.key ? 1 : -1));
  });

  constructor(
    @Inject(SESSIONS_SERVICE) private readonly sessionsApi: ISessionsService,
    @Inject(EQUIPMENT_SERVICE) private readonly equipmentApi: IEquipmentService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.equipmentApi.list().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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

  protected newSessionWithEquipment(id: string): void {
    this.router.navigate(['/workouts', 'new'], { queryParams: { equipment: id } });
  }

  protected goHome(): void {
    this.router.navigate(['/dashboard']);
  }

  protected readonly emptyEquipment = [
    { id: 'Treadmill', label: 'Treadmill', icon: 'directions_run', tint: 'workout-list__empty-tile-icon--green' },
    { id: 'IndoorBike', label: 'Indoor bike', icon: 'directions_bike', tint: 'workout-list__empty-tile-icon--amber' },
    { id: 'BenchPress', label: 'Bench press', icon: 'fitness_center', tint: 'workout-list__empty-tile-icon--orange' },
    { id: 'Elliptical', label: 'Elliptical', icon: 'fitness_center', tint: 'workout-list__empty-tile-icon--blue' }
  ];

  protected iconFor(equipment: string): string {
    return EQUIPMENT_ICON[equipment] ?? 'fitness_center';
  }

  protected tintFor(equipment: string): string {
    return EQUIPMENT_TINT[equipment] ?? 'workout-list__row-icon--green';
  }

  protected labelFor(equipment: string): string {
    return EQUIPMENT_LABEL[equipment] ?? equipment;
  }

  protected pointsFor(s: Session): number {
    return (s.durationMinutes ?? 0) * 2;
  }

  protected timeOfDay(s: Session): string {
    return new Date(s.startedAt).toLocaleTimeString(undefined, {
      hour: 'numeric',
      minute: '2-digit'
    });
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
    this.sessionsApi.list(query).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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

function startOfDay(d: Date): Date {
  const out = new Date(d);
  out.setHours(0, 0, 0, 0);
  return out;
}
