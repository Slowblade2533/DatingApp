import { inject } from '@angular/core';
import { ResolveFn, Router } from '@angular/router';
import { catchError, EMPTY } from 'rxjs';
import { MemberService } from '../../core/services/member.service';
import { Member } from '../../types/member';

export const memberResolver: ResolveFn<Member> = (route, state) => {
  const memberService = inject(MemberService);
  const router = inject(Router);
  const memberId = route.paramMap.get('id');

  if (!memberId) {
    router.navigateByUrl('/not-found');
    return EMPTY;
  }

  return memberService.getMember(memberId).pipe(
    catchError(() => {
      router.navigateByUrl('/not-found');
      return EMPTY;
    }),
  );
};
