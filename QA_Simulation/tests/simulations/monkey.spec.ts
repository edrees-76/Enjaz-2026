import { test, expect } from '@playwright/test';
import { LoginPage } from '../pom/LoginPage';

test.describe('Monkey Simulation (Random Interaction)', () => {
    test('Spamming Invalid Inputs', async ({ page }) => {
        const loginPage = new LoginPage(page);
        await loginPage.navigate();

        const randomStrings = ['!@#$%^&*', 'DROP TABLE Users;', 'VERY_LONG_STRING_'.repeat(20), '   '];

        for (const str of randomStrings) {
            await loginPage.login(str, 'wrong_pass');
            await loginPage.expectError('بيانات الدخول غير صحيحة');
        }
    });

    test('UI Stability under Rapid Clicks', async ({ page }) => {
        await page.goto('/');
        const loginBtn = page.locator('button:has-text("دخول")');

        // Rapid spam clicking to check for race conditions
        for (let i = 0; i < 10; i++) {
            loginBtn.click({ noWaitAfter: true });
        }

        // Ensure UI is still responsive and showing busy state/loader
        const loader = page.locator('.animate-spin');
        await expect(loader).toBeVisible();
    });
});
