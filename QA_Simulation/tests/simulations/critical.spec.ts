import { test, expect } from '@playwright/test';
import { LoginPage } from '../pom/LoginPage';

test.describe('Critical Path Simulation', () => {
    test('Successful Login Flow', async ({ page }) => {
        const loginPage = new LoginPage(page);
        await loginPage.navigate();

        // Visual Baseline Check
        await loginPage.captureVisualBaseline('login-initial');

        const latency = await loginPage.login('admin', '12345');

        // Performance Latency Check
        expect(latency).toBeLessThan(2000);

        // Verify success (in our twin we show an alert)
        page.on('dialog', async dialog => {
            expect(dialog.message()).toContain('Login Successful');
            await dialog.accept();
        });
    });
});
