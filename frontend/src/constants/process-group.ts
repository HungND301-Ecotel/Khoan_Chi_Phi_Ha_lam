export const ProcessGroupType = {
	None: 0,
	DL: 1,
	LC: 2,
	XL: 3,
} as const;

export type ProcessGroupType =
	(typeof ProcessGroupType)[keyof typeof ProcessGroupType];

export const PROCESS_GROUP: Record<string, string> = {
	TUNNELING: 'DL',
	LONGWALL: 'LC',
	ROADWAY_SLASHING: 'XL',
};

/**
 * Convert process group code (e.g., 'DL', 'LC', 'XL') to ProcessGroupType enum
 * @param code - The process group code
 * @returns The corresponding ProcessGroupType enum value
 */
export function getProcessGroupType(code?: string): ProcessGroupType {
	switch (code) {
		case PROCESS_GROUP.TUNNELING:
		case 'DL':
			return ProcessGroupType.DL;
		case PROCESS_GROUP.LONGWALL:
		case 'LC':
			return ProcessGroupType.LC;
		case PROCESS_GROUP.ROADWAY_SLASHING:
		case 'XL':
			return ProcessGroupType.XL;
		default:
			return ProcessGroupType.None;
	}
}
