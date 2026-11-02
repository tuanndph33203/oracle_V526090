import { createAction, props } from '@ngrx/store';

const BranchFilterAction = {
    setAvailableBranches: createAction('[Branch Filter] Set Available Branches', props<{ codes: string[] }>()),
    setSelectedBranches: createAction('[Branch Filter] Set Selected Branches', props<{ codes: { Code : string , Name : string}[] }>()),
    loadAvailableBranches: createAction('[Branch] Load Available Branches', props<{ codes: string[] }>()),
    loadAvailableBranchesSuccess: createAction('[Branch] Load Available Branches Success', props<{ branches: { Code: string; Name: string }[],codes: { Code: string; Name: string }[] }>()),
    reset: createAction('[Branch Filter] Reset')
};

export default BranchFilterAction;
