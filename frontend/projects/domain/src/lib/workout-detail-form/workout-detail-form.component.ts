import { Component, EventEmitter, Inject, OnInit, Output, computed, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import {
  CreateSessionRequest,
  EQUIPMENT_SERVICE,
  EquipmentItem,
  EquipmentType,
  IEquipmentService,
  ISessionsService,
  SESSIONS_SERVICE
} from 'api';
import { ButtonComponent, CardComponent } from 'components';

@Component({
  selector: 'forge-workout-detail-form',
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    CardComponent,
    ButtonComponent
  ],
  templateUrl: './workout-detail-form.component.html',
  styleUrl: './workout-detail-form.component.scss'
})
export class WorkoutDetailFormComponent implements OnInit {
  @Output() readonly created = new EventEmitter<{ id: string }>();

  protected readonly equipment = signal<EquipmentItem[]>([]);
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form;
  protected readonly equipmentValue = signal<EquipmentType>('Treadmill');
  protected readonly hideDistance = computed(() => this.equipmentValue() === 'BenchPress');

  constructor(
    private readonly fb: FormBuilder,
    @Inject(SESSIONS_SERVICE) private readonly sessions: ISessionsService,
    @Inject(EQUIPMENT_SERVICE) private readonly equipmentApi: IEquipmentService
  ) {
    const now = new Date();
    const isoDate = now.toISOString().slice(0, 10);
    const isoTime = now.toTimeString().slice(0, 5);

    this.form = this.fb.nonNullable.group({
      equipment: this.fb.nonNullable.control<EquipmentType>('Treadmill', Validators.required),
      date: this.fb.nonNullable.control(isoDate, Validators.required),
      time: this.fb.nonNullable.control(isoTime, Validators.required),
      durationMinutes: this.fb.nonNullable.control(22, [
        Validators.required,
        Validators.min(1),
        Validators.max(480)
      ]),
      distanceMiles: this.fb.control<number | null>(null, [Validators.min(0)]),
      avgHeartRateBpm: this.fb.control<number | null>(null, [Validators.min(30), Validators.max(240)]),
      activeCalories: this.fb.nonNullable.control(0, [
        Validators.required,
        Validators.min(0),
        Validators.max(5000)
      ]),
      notes: this.fb.control<string | null>(null, [Validators.maxLength(2000)])
    });

    this.form.controls.equipment.valueChanges.subscribe((value) => {
      this.equipmentValue.set(value);
    });
  }

  ngOnInit(): void {
    this.equipmentApi.list().subscribe({
      next: (items) => this.equipment.set(items),
      error: () => undefined
    });
  }

  protected onSubmit(): void {
    if (this.form.invalid || this.submitting()) {
      return;
    }
    this.submitting.set(true);
    this.errorMessage.set(null);

    const value = this.form.getRawValue();
    const startedAt = new Date(`${value.date}T${value.time}`).toISOString();
    const distance = this.hideDistance() ? null : value.distanceMiles;

    const request: CreateSessionRequest = {
      equipment: value.equipment,
      startedAt,
      durationMinutes: value.durationMinutes,
      distanceMiles: distance ?? null,
      avgHeartRateBpm: value.avgHeartRateBpm ?? null,
      activeCalories: value.activeCalories,
      notes: value.notes && value.notes.length > 0 ? value.notes : null
    };

    this.sessions.create(request).subscribe({
      next: (result) => {
        this.submitting.set(false);
        this.created.emit(result);
      },
      error: (err) => {
        this.submitting.set(false);
        this.errorMessage.set(err?.error?.title ?? 'Could not save session.');
      }
    });
  }
}
