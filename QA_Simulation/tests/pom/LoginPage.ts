import { Page, expect } from '@playwright/test';
import { AutoHeal } from '../utils/AutoHeal';

export class LoginPage {
    readonly page: Page;

    constructor(page: Page) {
        this.page = page;
    }

    async navigate() {
        await this.page.goto('/');
    }

    async login(username: string, password: string) {
        const userField = await AutoHeal.getSelector(this.page, 'input[placeholder="أدخل اسم المستخدم"]', ['input[type="text"]', '.input-field:first-child']);
        const passField = await AutoHeal.getSelector(this.page, 'input[placeholder="••••••••"]', ['input[type="password"]', '.input-field:last-child']);
        const submitBtn = await AutoHeal.getSelector(this.page, 'button:has-text("دخول")', ['.btn-primary', 'button[type="submit"]']);

        await userField.fill(username);
        await passField.fill(password);

        const startTime = Date.now();
        await submitBtn.click();
        const duration = Date.now() - startTime;

        if (duration > 2000) {
            console.warn(`[Latency Alert] Login action took ${duration}ms`);
        }

        return duration;
    }

    async expectError(message: string) {
        const errorBox = this.page.locator('.text-red-400');
        await expect(errorBox).toContainText(message);
    }

    async captureVisualBaseline(name: string) {
        await expect(this.page).toHaveScreenshot(`${name}.png`);
    }
}
