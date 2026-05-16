import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-star-fav-button',
  imports: [],
  templateUrl: './star-fav-button.html',
  styleUrl: './star-fav-button.css',
})
export class StarFavButton {
  disabled = input<boolean>();
  selected = input<boolean>();
  clickEvent = output<Event>();

  onClick(event: Event) {
    this.clickEvent.emit(event);
  }
}
