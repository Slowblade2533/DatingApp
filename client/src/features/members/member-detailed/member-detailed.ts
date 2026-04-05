import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  ActivatedRoute,
  NavigationEnd,
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet,
} from '@angular/router';
import { filter, map, startWith } from 'rxjs';
import { Member } from '../../../types/member';

@Component({
  selector: 'app-member-detailed',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './member-detailed.html',
  styleUrl: './member-detailed.css',
})
export class MemberDetailed {
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  // toSignal จัดการ subscribe/unsubscribe ให้อัตโนมัติ
  protected member = toSignal(this.route.data.pipe(map((data) => data['member'] as Member)));

  protected title = toSignal(
    this.router.events.pipe(
      filter((e) => e instanceof NavigationEnd),
      startWith(null),
      map(() => this.route.firstChild?.snapshot?.title ?? 'Profile'),
    ),
    { initialValue: 'Profile' },
  );
}
