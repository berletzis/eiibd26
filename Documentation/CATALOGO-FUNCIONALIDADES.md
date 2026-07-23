# Catálogo de funcionalidades — EIIBD

Lista simple de todo lo que la plataforma ya tiene, por quién lo usa. Para acordarte de con qué cuentas y qué empezar a explotar.
*(Compilado de las instrucciones del proyecto + wiki técnica + sesión 22-23 JUL 2026.)*

---

## 👤 Visitante anónimo (público, sin cuenta)
- **Home** y navegación pública.
- **Glosario de términos EII** — definiciones, con validación médica y artículos relacionados.
- **Preguntas y Respuestas públicas** — con respuestas de la IA NINA.
- **Directorio de médicos / profesionales de la salud** + mapa + perfil público del profesional.
- **Catálogo de platillos** navegable + **filtro por lo que no toleras**.
- **Fichas de ingredientes** con notas clínicas.
- **Encuesta de tolerancia** (`/tolero/{slug}`) — votar Sí / A veces / No y ver el % de la comunidad.
- **Artículos / contenidos** educativos.
- SEO: sitemaps, URLs limpias, códigos cortos.

## 🩺 Paciente con cuenta
- **Dashboard de salud** personal.
- **Registro de condiciones, síntomas y tratamientos.**
- **Diario de ánimo (mood)** diario.
- **Estadísticas de salud** — tendencias, síntoma más frecuente, perfil de dolor, insights.
- **Resumen médico en PDF** para llevar a consulta.
- **Perfil de alimentación** — declara lo que no toleras (alimenta el filtro de platillos).
- **Registro de laboratorios / resultados.**
- **Q&A con votos** + asistente NINA.
- **Perfil público** propio.
- **Notificaciones push.**

## 👨‍⚕️ Médico / profesional de la salud
- **Registro como profesional** (`/profesionaldelasalud/invitacion`) — arranca como pendiente.
- **Reclamar / gestionar su perfil** en el directorio.
- **Campo Título** (Dr., Dra., Nut., Lic., …) para cómo aparece.
- **Validar contenido** — glosario, notas clínicas de platillos, artículos, respuestas.
- **Badges** (perfil reclamado, verificado) que revelan su nombre en las validaciones.
- **Dashboard médico.**

## 🛠️ Administrador
- **CRUD de contenidos**, banners, condiciones, síntomas, tratamientos, glosario.
- **Gestión de usuarios.**
- **Gestión de médicos/directorio** — aprobar validadores, otorgar badges, verificar cédula.
- **Filtro de profesionales pendientes** de aprobación.
- **Push notifications** y sitemaps.
- **Panel de API keys** (solo lectura, enmascarado).
- **Motor de Cobertura** — qué temas ya cubres vs. lo que hay en internet.
- **Vista Oportunidades de contenido** (3 lentes: escribir nuevo / ampliar / mejorar).
- **Estadísticas de tolerancia** (bayesiano por tipo de EII) + **copiar liga de encuesta** + **control de envíos** (marcar enviada / pendientes).
- **Notas clínicas de platillos** — generarlas con IA, validación médica y candado de publicación.
- **Generación de contenido con IA** para síntomas, tratamientos, platillos e ingredientes.

## 🧠 Inteligencia detrás (los diferenciadores)
- **NINA** — asistente IA para preguntas (enruta Sonnet/Haiku/plantillas, filtros de seguridad clínica, caché de preguntas parecidas).
- **Motor de Cobertura** — embeddings Voyage + similitud semántica; mide qué tan bien cubres cada tema.
- **NINA Radar / crawler** — vigila fuentes externas de confianza (Educa Inflamatoria, funeiico, MyCrohns, y CCF por activar).
- **Referencias por recuperación** — para las notas, links reales recuperados del índice (nunca inventados).
- **Modelo bayesiano de tolerancia** (Beta-Binomial) — "X % de pacientes con [EII] toleran [alimento]", con honestidad de incertidumbre.
- **GRIS** — evaluador de calidad editorial de artículos (rúbrica de 7 aspectos con IA).
- **Consenso médico del glosario** + sistema de badges.

---

## ✅ Listo para empezar a usar YA
Cosas construidas que quizá no estás explotando todavía:
- **Campaña de encuestas de tolerancia** — copiar la liga por ingrediente desde el admin, mandarla, y llevar control de lo enviado. Cada voto alimenta el bayesiano.
- **Vista Oportunidades de contenido** — te dice qué artículos escribir/ampliar según los huecos vs. internet.
- **Generar notas de platillos/ingredientes con IA** + hacer que un nutriólogo las valide (ya tienes el alta de profesionales).
- **Resumen médico en PDF** — que los pacientes lo lleven a consulta.

## 💡 Ideas anotadas (aún NO construidas)
- Nutrientes ↔ ingredientes (solo rebanada EII, sin calorías) — idea futura.
- Conectar la encuesta de tolerancia con "agregar a mi lista de alimentos" del perfil (hoy son dos cosas separadas).
