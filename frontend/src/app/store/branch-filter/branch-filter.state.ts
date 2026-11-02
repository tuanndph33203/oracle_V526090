export interface IBranchFilterState {
    availableBranchCodes: { Code: string; Name: string }[];
    selectedBranchCodes: { Code: string; Name: string }[];
}
export const initialBranchFilterState: IBranchFilterState = {
    availableBranchCodes: [],
    selectedBranchCodes: []
};
