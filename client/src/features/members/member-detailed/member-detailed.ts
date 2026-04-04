import { Component, DestroyRef, inject, input, OnInit, signal } from '@angular/core';
import { MemberService } from '../../../core/services/member.service';
import {
  ActivatedRoute,
  NavigationEnd,
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet,
} from '@angular/router';
import { filter, switchMap } from 'rxjs';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-member-detailed',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './member-detailed.html',
  styleUrl: './member-detailed.css',
})
export class MemberDetailed implements OnInit {
  // รับ id จาก URL path 'members/:id' อัตโนมัติ
  id = input.required<string>();

  private memberService = inject(MemberService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);

  // สร้าง Observable ที่จะทำงานใหม่ทุกครั้งที่ id() เปลี่ยนค่า
  protected memberSignal = toSignal(
    toObservable(this.id).pipe(switchMap((id) => this.memberService.getMember(id))),
    { initialValue: null },
  );

  protected title = signal<string | undefined>('Profile');

  ngOnInit(): void {
    // อัปเดต Title เมื่อมีการเปลี่ยน Route ภายใน (Child Routes)
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.title.set(this.route.firstChild?.snapshot?.title);
        },
      });
  }
}
