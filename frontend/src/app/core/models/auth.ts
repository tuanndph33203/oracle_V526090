export interface IEmployeeInfo {
    Code: string;
    FullName: string;
}

export interface AuthResponse extends IUser {
    accessToken: string;
    refreshToken: string;
    accessTokenExpiresAt: string;
    refreshTokenExpiresAt: string;
    isSuccess: boolean;
    message: string;
}

export interface IAuthState {
    user: IUser | null;
    employee?: IEmployeeInfo | null;
    accessToken: string | null;
    refreshToken: string | null;
    loginError: string | null;
}

export interface IUser {
    userId: string;
    userName: string;
    email: string;
    code?: string;
    roles: any | null;
    permissions: string[] | null;
    applyBranchCodes: string[] | null;
    applyBranchCodesFilter?: string[] | null;
    firstName: string;
    lastName: string;
}
