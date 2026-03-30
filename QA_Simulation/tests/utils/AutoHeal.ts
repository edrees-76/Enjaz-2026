import { Page, Locator } from '@playwright/test';

/**
 * AutoHeal utility to handle dynamic selector changes.
 * If the primary selector fails, it tries fallback selectors.
 */
export class AutoHeal {
    static async getSelector(page: Page, primary: string, fallbacks: string[]): Promise<Locator> {
        const primaryLocator = page.locator(primary);
        try {
            await primaryLocator.waitFor({ timeout: 2000 });
            return primaryLocator;
        } catch {
            console.warn(`[AutoHeal] Primary selector '${primary}' failed. Trying fallbacks...`);
            for (const fallback of fallbacks) {
                const fallbackLocator = page.locator(fallback);
                try {
                    await fallbackLocator.waitFor({ timeout: 1000 });
                    console.info(`[AutoHeal] Successfully healed using fallback: '${fallback}'`);
                    return fallbackLocator;
                } catch {
                    continue;
                }
            }
            throw new Error(`[AutoHeal] All selectors failed for: ${primary}`);
        }
    }
}
