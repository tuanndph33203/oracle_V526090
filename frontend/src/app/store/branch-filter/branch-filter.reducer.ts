import { Action, createReducer, on } from '@ngrx/store';
import { IBranchFilterState, initialBranchFilterState } from './branch-filter.state';
import BranchFilterAction from './branch-filter.actions';

const reducer = createReducer(
    initialBranchFilterState,

    on(BranchFilterAction.loadAvailableBranchesSuccess, (state, { branches, codes }) => {
        return {
            ...state,
            availableBranchCodes: branches,
            selectedBranchCodes: codes.length ? codes : state.selectedBranchCodes
        };
    }),

    on(BranchFilterAction.setSelectedBranches, (state, { codes }) => ({
        ...state,
        selectedBranchCodes: codes.length ? codes : state.selectedBranchCodes
    })),

    on(BranchFilterAction.reset, () => initialBranchFilterState)
);

export function branchFilterReducer(state: IBranchFilterState | undefined, action: Action) {
    return reducer(state, action);
}
