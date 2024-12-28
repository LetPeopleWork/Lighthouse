import { expect } from "@playwright/test";

export function expectDateToBeRecent(received: Date, slack: number = 3000): void {
    const now = new Date();

    expect(Math.abs(received.getUTCMilliseconds() - now.getUTCMilliseconds())).toBeLessThanOrEqual(slack);
};

export function getLastUpdatedDateFromText(lastUpdatedText: string): Date {
    const dateMatch = /Last Updated on (.*)/.exec(lastUpdatedText);
    if (!dateMatch) {
        return new Date();
    }

    return new Date(dateMatch[1]);
};