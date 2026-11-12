import type { Metadata } from "next";
import "./globals.css";
export const metadata: Metadata = {
  title: "ChestTracker · Private Statistiken",
  description:
    "Deine Matches. Eure Statistiken. Privater ChestTracker für vier Freunde.",
  robots: { index: false, follow: false },
};
export default function Layout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="de">
      <body>{children}</body>
    </html>
  );
}
