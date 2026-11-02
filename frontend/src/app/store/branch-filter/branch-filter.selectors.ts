import { createSelector } from '@ngrx/store';

export const selectBranchFilter = (state: any) => state.branchFilter;

export const selectAvailableBranchCodes = createSelector(selectBranchFilter, (state) => state.availableBranchCodes);

export const selectSelectedBranchCodes = createSelector(selectBranchFilter, (state) => state.selectedBranchCodes);
