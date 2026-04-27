import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { PropOpsApiService } from '../core/propops-api.service';
import {
  MaintenanceOperationsDetail,
  MaintenanceDispatchOutcome,
  MaintenanceRequestCategory,
  MaintenanceRequestPriority,
  MaintenanceTriageInferenceResult,
  MaintenanceTriageOutputContract
} from '../models/propops.models';

@Component({
  selector: 'app-operations-detail-page',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './operations-detail-page.component.html',
  styleUrl: './operations-detail-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OperationsDetailPageComponent implements OnInit {
  private readonly api = inject(PropOpsApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly categories: MaintenanceRequestCategory[] = [
    'Plumbing',
    'Electrical',
    'HVAC',
    'Appliances',
    'Security',
    'General'
  ];
  protected readonly priorities: MaintenanceRequestPriority[] = ['Low', 'Normal', 'High', 'Emergency'];
  protected readonly dispatchOutcomes: MaintenanceDispatchOutcome[] = [
    'Completed',
    'Escalated',
    'Duplicate',
    'Cancelled',
    'NoAccess',
    'VendorUnavailable',
    'TenantResolved',
    'NotMaintenance'
  ];

  protected readonly detail = signal<MaintenanceOperationsDetail | null>(null);
  protected readonly inference = signal<MaintenanceTriageInferenceResult | null>(null);
  protected readonly loading = signal(true);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly success = signal<string | null>(null);

  protected readonly reviewForm = this.formBuilder.nonNullable.group({
    category: ['General' as MaintenanceRequestCategory, Validators.required],
    priority: ['Normal' as MaintenanceRequestPriority, Validators.required],
    vendorType: ['', Validators.required],
    dispatchDecision: ['', Validators.required],
    internalSummary: ['', Validators.required],
    tenantResponseDraft: ['', Validators.required]
  });

  protected readonly actionForm = this.formBuilder.nonNullable.group({
    workOrderSummary: [''],
    vendorName: [''],
    tenantMessage: [''],
    internalNote: ['']
  });

  protected readonly feedbackForm = this.formBuilder.nonNullable.group({
    finalResolution: ['', Validators.required],
    correctedCategory: ['General' as MaintenanceRequestCategory, Validators.required],
    correctedPriority: ['Normal' as MaintenanceRequestPriority, Validators.required],
    finalTenantResponse: ['', Validators.required],
    dispatchOutcome: ['Completed' as MaintenanceDispatchOutcome, Validators.required],
    resolutionNotes: [''],
    excludeFromTraining: [false],
    exclusionReason: ['']
  });

  private maintenanceRequestId = '';

  ngOnInit(): void {
    this.maintenanceRequestId = this.route.snapshot.paramMap.get('id') ?? '';
    this.loadDetail();
  }

  protected runInference(): void {
    this.busy.set(true);
    this.error.set(null);
    this.success.set(null);

    this.api
      .inferMaintenanceTriage(this.maintenanceRequestId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.inference.set(result);
          this.patchReviewForm(result.outputContract);
          this.busy.set(false);
          this.success.set('AI triage draft is ready for staff review.');
        },
        error: () => {
          this.error.set('Unable to run AI triage right now.');
          this.busy.set(false);
        }
      });
  }

  protected submitReview(): void {
    const currentInference = this.inference();
    if (!currentInference || this.reviewForm.invalid) {
      this.reviewForm.markAllAsTouched();
      return;
    }

    const formValue = this.reviewForm.getRawValue();
    this.busy.set(true);
    this.error.set(null);
    this.success.set(null);

    this.api
      .submitMaintenanceTriageReview(this.maintenanceRequestId, {
        aiOutput: currentInference.outputContract,
        guardrails: currentInference.guardrails,
        category: formValue.category,
        priority: formValue.priority,
        vendorType: formValue.vendorType,
        dispatchDecision: formValue.dispatchDecision,
        internalSummary: formValue.internalSummary,
        tenantResponseDraft: formValue.tenantResponseDraft
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (detail) => {
          this.applyDetail(detail);
          this.busy.set(false);
          this.success.set('Reviewed triage output saved.');
        },
        error: () => {
          this.error.set('Unable to save the triage review.');
          this.busy.set(false);
        }
      });
  }

  protected createWorkOrder(): void {
    const summary = this.actionForm.controls.workOrderSummary.value || this.reviewForm.controls.internalSummary.value;
    this.runAction(
      this.api.createWorkOrder(this.maintenanceRequestId, summary),
      'Work order created.'
    );
  }

  protected assignVendor(): void {
    const vendorName = this.actionForm.controls.vendorName.value || this.reviewForm.controls.vendorType.value;
    this.runAction(
      this.api.assignVendor(this.maintenanceRequestId, vendorName),
      'Vendor assignment logged.'
    );
  }

  protected notifyTenant(): void {
    const message = this.actionForm.controls.tenantMessage.value || this.reviewForm.controls.tenantResponseDraft.value;
    this.runAction(
      this.api.notifyTenant(this.maintenanceRequestId, message),
      'Tenant notification logged.'
    );
  }

  protected logInternalNote(): void {
    this.runAction(
      this.api.logInternalNote(this.maintenanceRequestId, this.actionForm.controls.internalNote.value),
      'Internal note logged.'
    );
  }

  protected submitResolutionFeedback(): void {
    if (this.feedbackForm.invalid) {
      this.feedbackForm.markAllAsTouched();
      return;
    }

    this.runAction(
      this.api.submitResolutionFeedback(this.maintenanceRequestId, this.feedbackForm.getRawValue()),
      'Resolution feedback saved and dataset eligibility evaluated.'
    );
  }

  protected trackByAction(_: number, action: { id: string }): string {
    return action.id;
  }

  private loadDetail(): void {
    this.api
      .getMaintenanceOperationsDetail(this.maintenanceRequestId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (detail) => {
          this.applyDetail(detail);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Unable to load this maintenance operation.');
          this.loading.set(false);
        }
      });
  }

  private runAction(request$: ReturnType<PropOpsApiService['createWorkOrder']>, successMessage: string): void {
    this.busy.set(true);
    this.error.set(null);
    this.success.set(null);

    request$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (detail) => {
          this.applyDetail(detail);
          this.busy.set(false);
          this.success.set(successMessage);
        },
        error: () => {
          this.error.set('Unable to complete that operation action.');
          this.busy.set(false);
        }
      });
  }

  private applyDetail(detail: MaintenanceOperationsDetail): void {
    this.detail.set(detail);
    const latestReview = detail.latestReview;
    if (latestReview) {
      this.reviewForm.patchValue({
        category: latestReview.finalCategory,
        priority: latestReview.finalPriority,
        vendorType: latestReview.finalVendorType,
        dispatchDecision: latestReview.finalDispatchDecision,
        internalSummary: latestReview.finalInternalSummary,
        tenantResponseDraft: latestReview.finalTenantResponseDraft
      });
      this.actionForm.patchValue({
        workOrderSummary: latestReview.finalInternalSummary,
        vendorName: latestReview.finalVendorType,
        tenantMessage: latestReview.finalTenantResponseDraft
      });
      if (!detail.latestFeedback) {
        this.feedbackForm.patchValue({
          correctedCategory: latestReview.finalCategory,
          correctedPriority: latestReview.finalPriority,
          finalTenantResponse: latestReview.finalTenantResponseDraft
        });
      }
    }
    const latestFeedback = detail.latestFeedback;
    if (latestFeedback) {
      this.feedbackForm.patchValue({
        finalResolution: latestFeedback.finalResolution,
        correctedCategory: latestFeedback.correctedCategory,
        correctedPriority: latestFeedback.correctedPriority,
        finalTenantResponse: latestFeedback.finalTenantResponse,
        dispatchOutcome: latestFeedback.dispatchOutcome,
        resolutionNotes: latestFeedback.resolutionNotes,
        excludeFromTraining: latestFeedback.excludeFromTraining,
        exclusionReason: latestFeedback.exclusionReason
      });
    }
  }

  private patchReviewForm(output: MaintenanceTriageOutputContract): void {
    this.reviewForm.patchValue({
      category: output.category,
      priority: output.priority,
      vendorType: output.vendorType,
      dispatchDecision: output.dispatchDecision,
      internalSummary: output.internalSummary,
      tenantResponseDraft: output.tenantResponseDraft
    });
    this.actionForm.patchValue({
      workOrderSummary: output.internalSummary,
      vendorName: output.vendorType,
      tenantMessage: output.tenantResponseDraft
    });
  }
}
