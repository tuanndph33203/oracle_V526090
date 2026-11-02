import { AuthResponse, IEmployeeInfo } from '@/core/models/auth';
import { createAction, props } from '@ngrx/store';

const AuthAction = {
    requestLogin: createAction('[Auth] Login Request', props<{ email: string; password: string }>()),
    callbackLogin: createAction('[Auth] Callback Login', props<{ sectionId: string }>()),
    successLogin: createAction('[Auth] Login Success', props<AuthResponse>()),
    requestUser: createAction('[Auth] Request User Profile'),
    successUserProfile: createAction('[Auth] Success Employee Profile', props<{ employee: IEmployeeInfo }>()),
    successUser: createAction('[Auth] Success User Profile', props<{ user: any }>()),
    failure: createAction('[Auth] Request Failure', props<{ error: string }>()),
    logout: createAction('[Auth] Logout Request')
};

export default AuthAction;
