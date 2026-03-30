import { test, expect } from '@playwright/test';

test.describe('Security Simulation (Pentesting Lite)', () => {
    test('Bypass Login attempt via DOM modification', async ({ page }) => {
        await page.goto('/');

        // Simulate a malicious user trying to enable a hidden "Admin Dashboard" 
        // or bypassing the busy state to click buttons they shouldn't.

        const loginBtn = page.locator('button:has-text("دخول")');

        // Check if we can intercept the network or mock a successful response (logic theft)
        await page.route('**/login', route => route.fulfill({
            status: 200,
            body: JSON.stringify({ role: 'admin' }),
        }));

        await page.fill('input[type="text"]', 'attacker');
        await page.fill('input[type="password"]', 'any');
        await loginBtn.click();

        // In our Twin, even if response is mocked, the frontend logic should handle it.
        // We expect the system to maintain its integrity.
    });
});
