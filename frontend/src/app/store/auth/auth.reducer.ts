import { Action, createReducer, on } from '@ngrx/store';
import AuthAction from './auth.actions';
import { IAuthState } from '@/core/models/auth';

export const initialState: IAuthState = {
    user: null,
    accessToken: null,
    refreshToken: null,
    loginError: null
};

const reducer = createReducer(
    initialState,

    on(AuthAction.successLogin, (state, response) => {
        const { accessToken, refreshToken, ...rest } = response;
        return {
            ...state,
            accessToken,
            refreshToken,
            loginError: null
        };
    }),
    on(AuthAction.successUser, (state, { user }) => {
        return {
            ...state,
            user
        };
    }),
    on(AuthAction.successUserProfile, (state, { employee }) => ({
        ...state,
        employee
    })),

    on(AuthAction.failure, (state, { error }) => ({
        ...state,
        loginError: error,
        user: null,
        accessToken: null,
        refreshToken: null
    })),

    on(AuthAction.logout, () => initialState)
);

export function authReducer(state: IAuthState | undefined, action: Action) {
    return reducer(state, action);
}
