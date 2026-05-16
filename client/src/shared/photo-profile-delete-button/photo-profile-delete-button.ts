import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-photo-profile-delete-button',
  imports: [],
  templateUrl: './photo-profile-delete-button.html',
  styleUrl: './photo-profile-delete-button.css',
})
export class PhotoProfileDeleteButton {
  disabled = input<boolean>();
  clickEvent = output<Event>();

  onClick(event: Event) {
    this.clickEvent.emit(event);
  }
}
