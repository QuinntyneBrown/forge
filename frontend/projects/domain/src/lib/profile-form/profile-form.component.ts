import { Component, EventEmitter, Inject, Input, OnInit, Output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import {
  CurrentUser,
  IMeService,
  IProfileService,
  ME_SERVICE,
  PROFILE_SERVICE
} from 'api';
import { ButtonComponent, CardComponent } from 'components';

@Component({
  selector: 'forge-profile-form',
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    CardComponent,
    ButtonComponent
  ],
  templateUrl: './profile-form.component.html',
  styleUrl: './profile-form.component.scss'
})
export class ProfileFormComponent implements OnInit {
  @Input() user: CurrentUser | null = null;
  @Input() hideSubmit = false;
  @Input() hideDangerZone = false;
  @Input() hideFields = false;

  @Output() readonly deleted = new EventEmitter<void>();

  protected readonly form;
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly saved = signal(false);
  protected readonly submitting = signal(false);
  protected readonly confirmingDelete = signal(false);
  protected readonly deleting = signal(false);
  protected readonly deleteError = signal<string | null>(null);

  constructor(
    private readonly fb: FormBuilder,
    @Inject(ME_SERVICE) private readonly me: IMeService,
    @Inject(PROFILE_SERVICE) private readonly profile: IProfileService
  ) {
    this.form = this.fb.nonNullable.group({
      firstName: ['', [Validators.required, Validators.maxLength(64)]],
      lastName: ['', [Validators.required, Validators.maxLength(64)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
      units: ['Imperial' as 'Imperial' | 'Metric', [Validators.required]],
      timeZoneId: ['', [Validators.required, Validators.maxLength(64)]],
      dailyActiveCaloriesTarget: [1500, [Validators.required, Validators.min(100), Validators.max(10_000)]],
      dailyWorkoutMinutesTarget: [60, [Validators.required, Validators.min(0), Validators.max(480)]]
    });
  }

  ngOnInit(): void {
    if (this.user) {
      this.populate(this.user);
      return;
    }
    this.me.getMe().subscribe({
      next: (user) => this.populate(user)
    });
  }

  protected onSubmit(): void {
    if (this.form.invalid || this.submitting()) {
      return;
    }
    this.submitting.set(true);
    this.errorMessage.set(null);
    this.saved.set(false);
    this.profile.updateProfile(this.form.getRawValue()).subscribe({
      next: () => {
        this.submitting.set(false);
        this.saved.set(true);
      },
      error: (err) => {
        this.submitting.set(false);
        this.errorMessage.set(err?.error?.title ?? 'Could not save profile.');
      }
    });
  }

  protected requestDelete(): void {
    if (this.deleting()) {
      return;
    }
    this.deleteError.set(null);
    this.confirmingDelete.set(true);
  }

  protected cancelDelete(): void {
    if (this.deleting()) {
      return;
    }
    this.confirmingDelete.set(false);
  }

  protected confirmDelete(): void {
    if (this.deleting()) {
      return;
    }
    this.deleting.set(true);
    this.deleteError.set(null);
    this.me.deleteMe().subscribe({
      next: () => {
        this.deleting.set(false);
        this.confirmingDelete.set(false);
        this.deleted.emit();
      },
      error: (err) => {
        this.deleting.set(false);
        this.deleteError.set(err?.error?.title ?? 'Could not delete account.');
      }
    });
  }

  private populate(user: CurrentUser): void {
    this.form.setValue({
      firstName: user.firstName,
      lastName: user.lastName,
      email: user.email,
      units: user.units,
      timeZoneId: user.timeZoneId,
      dailyActiveCaloriesTarget: user.dailyActiveCaloriesTarget,
      dailyWorkoutMinutesTarget: user.dailyWorkoutMinutesTarget
    });
  }
}
