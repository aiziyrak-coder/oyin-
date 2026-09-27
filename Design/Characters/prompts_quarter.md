# 45° ko'rinishlar uchun prompt (ixtiyoriy, aylanishni silliqlash uchun)

ChatGPT'da yangi chat oching, avatarning reference rasmini (masalan `references/M1.png`) biriktiring va quyidagini yuboring.
Natijani `references/<ID>_quarter.png` nomi bilan saqlang (masalan `M1_quarter.png`) va
`python3 process_references.py <ID>` ni ishga tushiring.

```
Using the attached character reference sheet, create a new photorealistic image in landscape 3:2 format showing the same person twice, side by side, on the same plain light-grey studio background:
1) left figure: front three-quarter view, body turned 45 degrees so the person faces toward the right side of the image (halfway between the front view and the side view);
2) right figure: back three-quarter view, body turned 135 degrees (halfway between the side view and the back view).
Keep everything identical to the reference: same body and proportions, same hair, same clothes and shoes, same relaxed A-pose, and the same completely blank featureless face with no eyes, eyebrows, nose or mouth. Full body from the top of the head to the shoes in both figures, soft even studio lighting, sharp focus. No text, no labels.
```
