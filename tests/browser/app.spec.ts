import { test, expect } from "@playwright/test";
test("private login, dashboard, admin and logout", async ({ page }) => {
  await page.goto("/");
  await expect(
    page.getByRole("heading", { name: "Deine Runde wartet." }),
  ).toBeVisible();
  await page.getByLabel("Benutzername", { exact: true }).fill("Fufi");
  await page
    .getByLabel("Passwort", { exact: true })
    .fill("browser-test-password-only");
  await page.getByRole("button", { name: "Anmelden", exact: true }).click();
  await expect(
    page.getByRole("heading", { name: "Guten Tag, Fufi." }),
  ).toBeVisible();
  await expect(
    page.getByText("Hier beginnt deine Match-Historie."),
  ).toBeVisible();
  await page
    .getByRole("button", { name: "Admin", exact: false })
    .first()
    .click();
  await expect(
    page.getByRole("heading", { name: "Euer Kreis. Deine Verwaltung." }),
  ).toBeVisible();
  await page.getByRole("button", { name: "Spieler hinzufügen" }).click();
  await page.getByLabel("Benutzername", { exact: true }).fill("BrowserFriend");
  await page.getByLabel("Anzeigename", { exact: true }).fill("Testfreund");
  await page.getByRole("button", { name: "Konto erstellen" }).click();
  await expect(
    page.getByRole("heading", { name: "Einmaliges Startpasswort" }),
  ).toBeVisible();
  await page.getByRole("button", { name: "Sicher übernommen" }).click();
  await expect(page.getByText("Testfreund", { exact: true })).toBeVisible();
  await page.setViewportSize({ width: 390, height: 844 });
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth <= innerWidth,
    ),
  ).toBeTruthy();
  await page
    .getByRole("button", { name: "Konto von BrowserFriend löschen" })
    .click();
  await expect(
    page.getByRole("heading", { name: "BrowserFriend endgültig löschen?" }),
  ).toBeVisible();
  await page.getByRole("button", { name: "Abbrechen", exact: true }).click();
  await expect(page.getByText("Testfreund", { exact: true })).toBeVisible();
  await page
    .getByRole("button", { name: "Konto von BrowserFriend löschen" })
    .click();
  await page
    .getByRole("button", { name: "Konto endgültig löschen", exact: true })
    .click();
  await expect(
    page.getByText("Konto gelöscht.", { exact: true }),
  ).toBeVisible();
  await expect(page.getByText("Testfreund", { exact: true })).toHaveCount(0);
  await page.setViewportSize({ width: 1440, height: 1000 });
  await page.getByRole("button", { name: "Abmelden" }).click();
  await expect(
    page.getByRole("button", { name: "Anmelden", exact: true }),
  ).toBeVisible();
});
test("mobile login and navigation fit the viewport", async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("/");
  await page.getByLabel("Benutzername", { exact: true }).fill("Fufi");
  await page
    .getByLabel("Passwort", { exact: true })
    .fill("browser-test-password-only");
  await page.getByRole("button", { name: "Anmelden", exact: true }).click();
  await expect(
    page.getByRole("heading", { name: "Guten Tag, Fufi." }),
  ).toBeVisible();
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth <= innerWidth,
    ),
  ).toBeTruthy();
  await page
    .getByRole("navigation")
    .getByRole("button", { name: "Rangliste", exact: true })
    .click();
  await expect(
    page.getByRole("heading", { name: "Die Rangliste" }),
  ).toBeVisible();
  await page
    .getByRole("navigation")
    .getByRole("button", { name: "Profil", exact: true })
    .click();
  await expect(
    page.getByRole("button", { name: "Passwort ändern", exact: true }),
  ).toBeVisible();
});
test("anonymous statistics cannot be fetched", async ({ request }) => {
  const r = await request.get("/api/dashboard");
  expect(r.status()).toBe(401);
});
