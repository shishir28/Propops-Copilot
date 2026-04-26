import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { PropOpsApiService } from '../core/propops-api.service';
import {
  CreateMaintenanceRequestPayload,
  IntakeChannel,
  MaintenanceRequest,
  MaintenanceRequestCategory,
  MaintenanceRequestPriority
} from '../models/propops.models';

@Component({
  selector: 'app-intake-page',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './intake-page.component.html',
  styleUrl: './intake-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class IntakePageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly api = inject(PropOpsApiService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly categories: MaintenanceRequestCategory[] = [
    'Plumbing',
    'Electrical',
    'HVAC',
    'Appliances',
    'Security',
    'General'
  ];

  protected readonly priorities: MaintenanceRequestPriority[] = ['Low', 'Normal', 'High', 'Emergency'];
  protected readonly channels: IntakeChannel[] = ['Portal', 'Email', 'SmsChat', 'PhoneNote'];

  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly createdRequest = signal<MaintenanceRequest | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group({
    submitterName: ['', Validators.required],
    emailAddress: ['', [Validators.required, Validators.email]],
    phoneNumber: ['', Validators.required],
    propertyName: ['', Validators.required],
    unitNumber: [''],
    description: ['', [Validators.required, Validators.minLength(20)]],
    category: ['General' as MaintenanceRequestCategory, Validators.required],
    priority: ['Normal' as MaintenanceRequestPriority, Validators.required],
    channel: ['Portal' as IntakeChannel, Validators.required]
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    const payload = this.form.getRawValue() as CreateMaintenanceRequestPayload;

    this.api
      .createMaintenanceRequest(payload)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (request) => {
          this.createdRequest.set(request);
          this.saving.set(false);
          this.form.reset({
            submitterName: '',
            emailAddress: '',
            phoneNumber: '',
            propertyName: '',
            unitNumber: '',
            description: '',
            category: 'General',
            priority: 'Normal',
            channel: 'Portal'
          });
        },
        error: () => {
          this.error.set('The request could not be saved. Please try again.');
          this.saving.set(false);
        }
      });
  }
}
