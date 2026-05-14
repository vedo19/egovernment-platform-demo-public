export const SERVICE_REQUEST_STATUS = {
  SUBMITTED: 'Submitted',
  OFFICER_ASSIGNED: 'OfficerAssigned',
  AWAITING_DOCUMENTS: 'AwaitingDocuments',
  UNDER_REVIEW: 'UnderReview',
  DOCUMENTS_REJECTED: 'DocumentsRejected',
  APPROVED: 'Approved',
  REJECTED: 'Rejected',
};

export const DOCUMENT_STATUS = {
  SUBMITTED: 'Submitted',
  UNDER_REVIEW: 'UnderReview',
  APPROVED: 'Approved',
  REJECTED: 'Rejected',
};

export const LEGACY_STATUS = {
  PENDING: 'Pending',
  IN_PROGRESS: 'InProgress',
  PROCESSING: 'Processing',
};

export const SERVICE_REQUEST_STATUSES = Object.values(SERVICE_REQUEST_STATUS);

export const DOCUMENT_STATUSES = Object.values(DOCUMENT_STATUS);

export function isPendingStatus(status) {
  return [
    SERVICE_REQUEST_STATUS.SUBMITTED,
    SERVICE_REQUEST_STATUS.OFFICER_ASSIGNED,
    SERVICE_REQUEST_STATUS.AWAITING_DOCUMENTS,
    SERVICE_REQUEST_STATUS.UNDER_REVIEW,
    SERVICE_REQUEST_STATUS.DOCUMENTS_REJECTED,
    DOCUMENT_STATUS.SUBMITTED,
    DOCUMENT_STATUS.UNDER_REVIEW,
    LEGACY_STATUS.PENDING,
    LEGACY_STATUS.IN_PROGRESS,
    LEGACY_STATUS.PROCESSING,
  ].includes(status);
}

export function canUploadRequestDocument(status) {
  return [
    SERVICE_REQUEST_STATUS.AWAITING_DOCUMENTS,
    SERVICE_REQUEST_STATUS.DOCUMENTS_REJECTED,
  ].includes(status);
}

export function canDownloadDocument(status) {
  return status === DOCUMENT_STATUS.APPROVED;
}

export function isTerminalStatus(status) {
  return [
    SERVICE_REQUEST_STATUS.APPROVED,
    SERVICE_REQUEST_STATUS.REJECTED,
    DOCUMENT_STATUS.APPROVED,
    DOCUMENT_STATUS.REJECTED,
  ].includes(status);
}
