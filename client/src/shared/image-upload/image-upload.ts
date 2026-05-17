import { Component, input, output, signal } from '@angular/core';

@Component({
  selector: 'app-image-upload',
  imports: [],
  templateUrl: './image-upload.html',
  styleUrl: './image-upload.css',
})
export class ImageUpload {
  protected imageSrc = signal<string | ArrayBuffer | null | undefined>(null);
  protected validationError = signal<string | null>(null);
  protected isDragging = false;
  private fileToUpload: File | null = null;
  private readonly maxPhotoBytes = 5 * 1024 * 1024;
  private readonly allowedMimeTypes = new Set(['image/jpeg', 'image/png', 'image/webp']);

  uploadFile = output<File>();
  loading = input<boolean>(false);

  onDragOver(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = true;
  }

  onDragLeave(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;
  }

  onDrop(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;

    if (event.dataTransfer?.files.length) {
      const file = event.dataTransfer.files[0];
      this.handleSelectedFile(file);
    }
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.handleSelectedFile(file);
    input.value = '';
  }

  onCancel() {
    this.fileToUpload = null;
    this.imageSrc.set(null);
    this.validationError.set(null);
  }

  onUploadFile() {
    if (this.fileToUpload) {
      this.uploadFile.emit(this.fileToUpload);
    }
  }

  private handleSelectedFile(file: File) {
    if (!this.allowedMimeTypes.has(file.type)) {
      this.validationError.set('Only JPG, PNG, or WEBP images are allowed.');
      this.fileToUpload = null;
      this.imageSrc.set(null);
      return;
    }

    if (file.size > this.maxPhotoBytes) {
      this.validationError.set('Image is too large. Maximum size is 5 MB.');
      this.fileToUpload = null;
      this.imageSrc.set(null);
      return;
    }

    this.validationError.set(null);
    this.fileToUpload = file;
    this.previewImage(file);
  }

  private previewImage(file: File) {
    const reader = new FileReader();
    reader.onload = (e) => this.imageSrc.set(e.target?.result);
    reader.readAsDataURL(file);
  }
}
