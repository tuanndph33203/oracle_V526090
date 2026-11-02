export const formatVNDate = (date?: string) => {
    if (!date) return '';
    return new Intl.DateTimeFormat('vi-VN').format(new Date(date));
};
