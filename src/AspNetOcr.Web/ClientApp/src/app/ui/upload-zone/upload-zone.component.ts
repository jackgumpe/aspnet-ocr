import { Component, EventEmitter, Input, Output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';

@Component({
  selector: 'asp-upload-zone',
  standalone: true,
  imports: [MatButtonModule, MatProgressBarModule],
  template: `
    <section
      class="upload-zone"
      [class.upload-zone--active]="isDragging"
      [class.upload-zone--busy]="busy"
      [class.upload-zone--error]="error"
      (dragover)="onDragOver($event)"
      (dragleave)="onDragLeave()"
      (drop)="onDrop($event)">
      <input
        #fileInput
        class="visually-hidden"
        type="file"
        accept="application/pdf,image/png,image/jpeg,image/tiff"
        (change)="onFileInput($event)">
      <div class="upload-zone__content">
        <span class="upload-zone__glyph" aria-hidden="true">↑</span>
        <div>
          <h2>{{ selectedFileName || 'Drop PDF or image files' }}</h2>
          <p>{{ error || 'PDF, PNG, JPG, and TIFF are accepted for mock OCR processing.' }}</p>
        </div>
        <button mat-flat-button color="primary" type="button" class="focus-ring" (click)="fileInput.click()" [disabled]="busy">
          Choose file
        </button>
        @if (busy) {
          <p class="upload-zone__processing" role="status">Processing {{ progress }}%</p>
        }
      </div>
      @if (busy) {
        <mat-progress-bar mode="determinate" [value]="progress" aria-label="OCR upload progress"></mat-progress-bar>
      }
    </section>
  `,
  styleUrl: './upload-zone.component.scss'
})
export class UploadZoneComponent {
  @Input() busy = false;
  @Input() error = '';
  @Input() progress = 0;
  @Output() fileSelected = new EventEmitter<File>();

  selectedFileName = '';
  isDragging = false;

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragging = true;
  }

  onDragLeave(): void {
    this.isDragging = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragging = false;
    const file = event.dataTransfer?.files.item(0);
    if (file) {
      this.emitFile(file);
    }
  }

  onFileInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.item(0);
    if (file) {
      this.emitFile(file);
      input.value = '';
    }
  }

  private emitFile(file: File): void {
    this.selectedFileName = file.name;
    this.fileSelected.emit(file);
  }
}
