import { inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, of, exhaustMap, map, tap, switchMap, forkJoin } from 'rxjs';
import AuthAction from './auth.actions';
import BranchFilterAction from '../branch-filter/branch-filter.actions';
import { Auth } from '@/core/services/auth';
import { RouteService } from '@/core/services/route.service';
import { EmployeeService } from '@/features/employee/services/empoyee';
import { BranchService } from '@/features/branch/services/branch';

@Injectable()
export class AuthEffects {
    private actions = inject(Actions);
    private authService = inject(Auth);
    private routeServive = inject(RouteService);
    private branchService = inject(BranchService);
    private employeeService = inject(EmployeeService);
    private router = inject(Router);

    loginRequest = createEffect(() =>
        this.actions.pipe(
            ofType(AuthAction.requestLogin),
            switchMap(({ email, password }) =>
                this.authService.login(email, password).pipe(
                    map((response) => {
                        if (response.code === 200) {
                            return AuthAction.successLogin(response.data);
                        }
                        return AuthAction.failure(response.data);
                    }),
                    catchError((error) => {
                        return of(AuthAction.failure(error));
                    })
                )
            )
        )
    );

    callbackLogin = createEffect(() =>
        this.actions.pipe(
            ofType(AuthAction.callbackLogin),
            switchMap(({ sectionId }) =>
                this.authService.loginBySectionId(sectionId).pipe(
                    map((response) => {
                        return AuthAction.successLogin(response);
                    }),
                    catchError((error) => {
                        return of(AuthAction.failure(error));
                    })
                )
            )
        )
    );

    loginSuccess = createEffect(
        () =>
            this.actions.pipe(
                ofType(AuthAction.successLogin),
                tap((response) => {
                    localStorage.setItem('authToken', response.accessToken);
                    const url: string | null = this.routeServive.loadUrl();
                    this.router.navigateByUrl(url || '/');
                })
            ),
        { dispatch: false }
    );
    userRequest = createEffect(() =>
        this.actions.pipe(
            ofType(AuthAction.requestUser),
            switchMap(() =>
                this.authService.getUserProfile().pipe(
                    map((response) => {
                        if (response.code !== 200) {
                            return AuthAction.logout();
                        }
                        return AuthAction.successUser({ user: response.data });
                    }),
                    catchError((error) => of(AuthAction.failure({ error })))
                )
            )
        )
    );

    getUserProfile = createEffect(() =>
        this.actions.pipe(
            ofType(AuthAction.successUser),
            switchMap(({ user }) =>
                forkJoin([this.employeeService.getByCode(user?.code), this.branchService.getByFilter({ page: 1, size: 100 })]).pipe(
                    switchMap(([employeeRes, branchRes]) => {
                        const employee = employeeRes?.Data;
                        const content = (branchRes?.Data?.Content ?? []).filter((b: any) => b.Status === 1);
                        const rawApplyBranchCodes = user?.applyBranchCodes ?? user?.ApplyBranchCodes ?? [];
                        const applyBranchCodes: string[] = typeof rawApplyBranchCodes === 'string'
                            ? rawApplyBranchCodes.split(',').map((x: string) => x.trim()).filter((x: string) => x.length > 0)
                            : (Array.isArray(rawApplyBranchCodes) ? rawApplyBranchCodes : []);
                        const branches = (applyBranchCodes.includes('*') || applyBranchCodes.length === 0)
                            ? content
                            : content.filter((b: any) => applyBranchCodes.includes(b.Code));
                        const branchSelected = JSON.parse(localStorage.getItem('branchSelected') || '[]');
                        const matchedSelected = branchSelected.length ? branches.filter((b: any) => branchSelected.includes(b.Code)) : [];
                        const selectedBranches = matchedSelected.length ? matchedSelected : (branches.length ? [branches[0]] : []);
                        return [
                            AuthAction.successUserProfile({ employee }),
                            BranchFilterAction.loadAvailableBranchesSuccess({
                                branches,
                                codes: selectedBranches
                            })
                        ];
                    }),
                    catchError((error) => of(AuthAction.failure({ error })))
                )
            )
        )
    );

    logout = createEffect(
        () =>
            this.actions.pipe(
                ofType(AuthAction.logout),
                tap(() => {
                    localStorage.removeItem('authToken');
                    localStorage.removeItem('branchSelected');
                    this.router.navigateByUrl('/login');
                }),
                exhaustMap(async () => this.authService.logout())
            ),
        { dispatch: false }
    );

    resetBranchFilterOnLogout = createEffect(() =>
        this.actions.pipe(
            ofType(AuthAction.logout),
            map(() => BranchFilterAction.reset())
        )
    );

    saveSelectedBranches = createEffect(
        () =>
            this.actions.pipe(
                ofType(BranchFilterAction.setSelectedBranches),
                tap(({ codes }) => {
                    const selectedCodes = codes.map((code: any) => code.Code);
                    localStorage.setItem('branchSelected', JSON.stringify(selectedCodes));
                })
            ),
        { dispatch: false }
    );

    failure = createEffect(
        () =>
            this.actions.pipe(
                ofType(AuthAction.failure),
                tap(({ error }) => {
                    console.error('Auth Error:', error);
                })
            ),
        { dispatch: false }
    );
}
