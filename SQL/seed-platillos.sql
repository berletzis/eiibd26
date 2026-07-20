-- SQL/seed-platillos.sql
-- Carga inicial del módulo de Platillos. Se corre UNA sola vez.
-- Vocabulario controlado + 17 platillos capturados + 57 ingredientes + 99 relaciones.
-- Incluye las 3 correcciones de clasificación: atún->pescado, leche de coco->bebida, pollo->ave.
-- Idempotente: los INSERT están guardados con NOT EXISTS.
SET NOCOUNT ON;
GO

-- ============ 1) VOCABULARIO ============
IF NOT EXISTS(SELECT 1 FROM PlatGrupo WHERE Nombre=N'lácteo') INSERT PlatGrupo(Nombre,Orden,Activo) VALUES(N'lácteo',1,1);
IF NOT EXISTS(SELECT 1 FROM PlatGrupo WHERE Nombre=N'huevo') INSERT PlatGrupo(Nombre,Orden,Activo) VALUES(N'huevo',2,1);
IF NOT EXISTS(SELECT 1 FROM PlatGrupo WHERE Nombre=N'carne') INSERT PlatGrupo(Nombre,Orden,Activo) VALUES(N'carne',3,1);
IF NOT EXISTS(SELECT 1 FROM PlatGrupo WHERE Nombre=N'ave') INSERT PlatGrupo(Nombre,Orden,Activo) VALUES(N'ave',4,1);
IF NOT EXISTS(SELECT 1 FROM PlatGrupo WHERE Nombre=N'embutido') INSERT PlatGrupo(Nombre,Orden,Activo) VALUES(N'embutido',5,1);
IF NOT EXISTS(SELECT 1 FROM PlatGrupo WHERE Nombre=N'pescado') INSERT PlatGrupo(Nombre,Orden,Activo) VALUES(N'pescado',6,1);
IF NOT EXISTS(SELECT 1 FROM PlatGrupo WHERE Nombre=N'marisco') INSERT PlatGrupo(Nombre,Orden,Activo) VALUES(N'marisco',7,1);
IF NOT EXISTS(SELECT 1 FROM PlatGrupo WHERE Nombre=N'verdura') INSERT PlatGrupo(Nombre,Orden,Activo) VALUES(N'verdura',8,1);
IF NOT EXISTS(SELECT 1 FROM PlatGrupo WHERE Nombre=N'fruta') INSERT PlatGrupo(Nombre,Orden,Activo) VALUES(N'fruta',9,1);
IF NOT EXISTS(SELECT 1 FROM PlatGrupo WHERE Nombre=N'fruto-seco') INSERT PlatGrupo(Nombre,Orden,Activo) VALUES(N'fruto-seco',10,1);
IF NOT EXISTS(SELECT 1 FROM PlatGrupo WHERE Nombre=N'cereal') INSERT PlatGrupo(Nombre,Orden,Activo) VALUES(N'cereal',11,1);
IF NOT EXISTS(SELECT 1 FROM PlatGrupo WHERE Nombre=N'legumbre') INSERT PlatGrupo(Nombre,Orden,Activo) VALUES(N'legumbre',12,1);
IF NOT EXISTS(SELECT 1 FROM PlatGrupo WHERE Nombre=N'tubérculo') INSERT PlatGrupo(Nombre,Orden,Activo) VALUES(N'tubérculo',13,1);
IF NOT EXISTS(SELECT 1 FROM PlatGrupo WHERE Nombre=N'hongo') INSERT PlatGrupo(Nombre,Orden,Activo) VALUES(N'hongo',14,1);
IF NOT EXISTS(SELECT 1 FROM PlatGrupo WHERE Nombre=N'grasa') INSERT PlatGrupo(Nombre,Orden,Activo) VALUES(N'grasa',15,1);
IF NOT EXISTS(SELECT 1 FROM PlatGrupo WHERE Nombre=N'condimento') INSERT PlatGrupo(Nombre,Orden,Activo) VALUES(N'condimento',16,1);
IF NOT EXISTS(SELECT 1 FROM PlatGrupo WHERE Nombre=N'bebida') INSERT PlatGrupo(Nombre,Orden,Activo) VALUES(N'bebida',17,1);
IF NOT EXISTS(SELECT 1 FROM PlatGrupo WHERE Nombre=N'otro') INSERT PlatGrupo(Nombre,Orden,Activo) VALUES(N'otro',18,1);
IF NOT EXISTS(SELECT 1 FROM PlatAtributo WHERE Nombre=N'gluten') INSERT PlatAtributo(Nombre,Ambito,Activo) VALUES(N'gluten',N'Ingrediente',1);
IF NOT EXISTS(SELECT 1 FROM PlatAtributo WHERE Nombre=N'lactosa') INSERT PlatAtributo(Nombre,Ambito,Activo) VALUES(N'lactosa',N'Ingrediente',1);
IF NOT EXISTS(SELECT 1 FROM PlatAtributo WHERE Nombre=N'picante') INSERT PlatAtributo(Nombre,Ambito,Activo) VALUES(N'picante',N'Ingrediente',1);
IF NOT EXISTS(SELECT 1 FROM PlatAtributo WHERE Nombre=N'cítrico') INSERT PlatAtributo(Nombre,Ambito,Activo) VALUES(N'cítrico',N'Ingrediente',1);
IF NOT EXISTS(SELECT 1 FROM PlatAtributo WHERE Nombre=N'alcohol') INSERT PlatAtributo(Nombre,Ambito,Activo) VALUES(N'alcohol',N'Ingrediente',1);
IF NOT EXISTS(SELECT 1 FROM PlatAtributo WHERE Nombre=N'graso') INSERT PlatAtributo(Nombre,Ambito,Activo) VALUES(N'graso',N'Ingrediente',1);
IF NOT EXISTS(SELECT 1 FROM PlatAtributo WHERE Nombre=N'fibra-insoluble') INSERT PlatAtributo(Nombre,Ambito,Activo) VALUES(N'fibra-insoluble',N'Ingrediente',1);
IF NOT EXISTS(SELECT 1 FROM PlatAtributo WHERE Nombre=N'cafeína') INSERT PlatAtributo(Nombre,Ambito,Activo) VALUES(N'cafeína',N'Ingrediente',1);
IF NOT EXISTS(SELECT 1 FROM PlatAtributo WHERE Nombre=N'crudo') INSERT PlatAtributo(Nombre,Ambito,Activo) VALUES(N'crudo',N'Uso',1);
IF NOT EXISTS(SELECT 1 FROM PlatAtributo WHERE Nombre=N'frito') INSERT PlatAtributo(Nombre,Ambito,Activo) VALUES(N'frito',N'Uso',1);
IF NOT EXISTS(SELECT 1 FROM PlatAtributo WHERE Nombre=N'en jugo') INSERT PlatAtributo(Nombre,Ambito,Activo) VALUES(N'en jugo',N'Uso',1);
IF NOT EXISTS(SELECT 1 FROM PlatCategoria WHERE Nombre=N'Entrada') INSERT PlatCategoria(Nombre,Orden,Activo) VALUES(N'Entrada',1,1);
IF NOT EXISTS(SELECT 1 FROM PlatCategoria WHERE Nombre=N'Plato fuerte') INSERT PlatCategoria(Nombre,Orden,Activo) VALUES(N'Plato fuerte',2,1);
IF NOT EXISTS(SELECT 1 FROM PlatCategoria WHERE Nombre=N'Ensalada') INSERT PlatCategoria(Nombre,Orden,Activo) VALUES(N'Ensalada',3,1);
IF NOT EXISTS(SELECT 1 FROM PlatCategoria WHERE Nombre=N'Sopa') INSERT PlatCategoria(Nombre,Orden,Activo) VALUES(N'Sopa',4,1);
IF NOT EXISTS(SELECT 1 FROM PlatCategoria WHERE Nombre=N'Snack') INSERT PlatCategoria(Nombre,Orden,Activo) VALUES(N'Snack',5,1);
IF NOT EXISTS(SELECT 1 FROM PlatCategoria WHERE Nombre=N'Postre') INSERT PlatCategoria(Nombre,Orden,Activo) VALUES(N'Postre',6,1);
IF NOT EXISTS(SELECT 1 FROM PlatCategoria WHERE Nombre=N'Bebida') INSERT PlatCategoria(Nombre,Orden,Activo) VALUES(N'Bebida',7,1);
IF NOT EXISTS(SELECT 1 FROM PlatCategoria WHERE Nombre=N'Guarnición') INSERT PlatCategoria(Nombre,Orden,Activo) VALUES(N'Guarnición',8,1);
IF NOT EXISTS(SELECT 1 FROM PlatUnidad WHERE Nombre=N'pieza') INSERT PlatUnidad(Nombre,Activo) VALUES(N'pieza',1);
IF NOT EXISTS(SELECT 1 FROM PlatUnidad WHERE Nombre=N'taza') INSERT PlatUnidad(Nombre,Activo) VALUES(N'taza',1);
IF NOT EXISTS(SELECT 1 FROM PlatUnidad WHERE Nombre=N'g') INSERT PlatUnidad(Nombre,Activo) VALUES(N'g',1);
IF NOT EXISTS(SELECT 1 FROM PlatUnidad WHERE Nombre=N'kg') INSERT PlatUnidad(Nombre,Activo) VALUES(N'kg',1);
IF NOT EXISTS(SELECT 1 FROM PlatUnidad WHERE Nombre=N'ml') INSERT PlatUnidad(Nombre,Activo) VALUES(N'ml',1);
IF NOT EXISTS(SELECT 1 FROM PlatUnidad WHERE Nombre=N'l') INSERT PlatUnidad(Nombre,Activo) VALUES(N'l',1);
IF NOT EXISTS(SELECT 1 FROM PlatUnidad WHERE Nombre=N'cda') INSERT PlatUnidad(Nombre,Activo) VALUES(N'cda',1);
IF NOT EXISTS(SELECT 1 FROM PlatUnidad WHERE Nombre=N'cdta') INSERT PlatUnidad(Nombre,Activo) VALUES(N'cdta',1);
IF NOT EXISTS(SELECT 1 FROM PlatUnidad WHERE Nombre=N'al gusto') INSERT PlatUnidad(Nombre,Activo) VALUES(N'al gusto',1);
IF NOT EXISTS(SELECT 1 FROM PlatUnidad WHERE Nombre=N'paquete') INSERT PlatUnidad(Nombre,Activo) VALUES(N'paquete',1);
IF NOT EXISTS(SELECT 1 FROM PlatUnidad WHERE Nombre=N'bandeja') INSERT PlatUnidad(Nombre,Activo) VALUES(N'bandeja',1);
IF NOT EXISTS(SELECT 1 FROM PlatUnidad WHERE Nombre=N'tarro') INSERT PlatUnidad(Nombre,Activo) VALUES(N'tarro',1);
GO

-- ============ 2) INGREDIENTES ============
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'aceite')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'aceite',g.Id,N'',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'grasa';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'aceite de oliva')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'aceite de oliva',g.Id,N'',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'grasa';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'acelga')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'acelga',g.Id,N'',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'verdura';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'agua')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'agua',g.Id,N'',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'bebida';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'aguacate')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'aguacate',g.Id,N'grasa saludable, suele tolerarse',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'fruta';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'albahaca')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'albahaca',g.Id,N'',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'condimento';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'almendra')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'almendra',g.Id,N'',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'fruto-seco';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'apio')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'apio',g.Id,N'',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'verdura';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'arroz')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'arroz',g.Id,N'suele tolerarse bien',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'cereal';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'arveja')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'arveja',g.Id,N'',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'legumbre';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'atún')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'atún',g.Id,N'CORREGIDO: estaba como marisco',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'pescado';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'betabel')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'betabel',g.Id,N'',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'verdura';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'brioche')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'brioche',g.Id,N'pan; contiene gluten y lácteos',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'cereal';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'caldo de pollo')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'caldo de pollo',g.Id,N'',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'otro';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'camarón')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'camarón',g.Id,N'ojo si está crudo',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'marisco';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'cebolla')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'cebolla',g.Id,N'cruda irrita; cocida más tolerable',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'verdura';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'cebollín')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'cebollín',g.Id,N'',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'verdura';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'cerveza')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'cerveza',g.Id,N'',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'bebida';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'champiñón')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'champiñón',g.Id,N'',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'hongo';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'chiltepín')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'chiltepín',g.Id,N'muy irritante',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'condimento';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'cilantro')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'cilantro',g.Id,N'hierba',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'condimento';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'col')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'col',g.Id,N'cruda irrita en brote',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'verdura';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'coliflor')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'coliflor',g.Id,N'flatulenta',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'verdura';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'crema')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'crema',g.Id,N'intolerancia común',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'lácteo';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'durazno')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'durazno',g.Id,N'',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'fruta';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'fresa')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'fresa',g.Id,N'semillas',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'fruta';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'harina de trigo')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'harina de trigo',g.Id,N'',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'cereal';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'huevo')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'huevo',g.Id,N'',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'huevo';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'jalapeño')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'jalapeño',g.Id,N'irritante en brote',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'verdura';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'jengibre')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'jengibre',g.Id,N'',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'condimento';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'kiwi')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'kiwi',g.Id,N'',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'fruta';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'laurel')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'laurel',g.Id,N'',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'condimento';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'leche')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'leche',g.Id,N'',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'lácteo';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'leche de coco')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'leche de coco',g.Id,N'CORREGIDO: NO es lácteo; es sustituto de lácteos',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'bebida';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'lechuga')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'lechuga',g.Id,N'',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'verdura';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'limón')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'limón',g.Id,N'jugo ácido',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'fruta';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'manzana')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'manzana',g.Id,N'',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'fruta';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'mayonesa')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'mayonesa',g.Id,N'base huevo/aceite',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'grasa';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'melón')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'melón',g.Id,N'',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'fruta';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'naranja')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'naranja',g.Id,N'jugo ácido',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'fruta';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'pan pita')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'pan pita',g.Id,N'',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'cereal';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'papa')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'papa',g.Id,N'bien tolerada cocida',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'tubérculo';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'pepino')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'pepino',g.Id,N'',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'verdura';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'pera')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'pera',g.Id,N'',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'fruta';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'pescado blanco')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'pescado blanco',g.Id,N'proteína magra, suele tolerarse',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'pescado';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'pimienta')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'pimienta',g.Id,N'',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'condimento';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'pollo')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'pollo',g.Id,N'CORREGIDO: estaba como carne-roja',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'ave';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'queso')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'queso',g.Id,N'',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'lácteo';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'sal')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'sal',g.Id,N'',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'condimento';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'salsa de soya')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'salsa de soya',g.Id,N'alta en sodio; puede tener gluten',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'condimento';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'tocino')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'tocino',g.Id,N'graso y salado',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'embutido';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'tomate')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'tomate',g.Id,N'ácido',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'verdura';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'tortilla de maíz')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'tortilla de maíz',g.Id,N'sin gluten',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'cereal';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'tostada de maíz')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'tostada de maíz',g.Id,N'frita; sin gluten',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'cereal';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'yogur')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'yogur',g.Id,N'',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'lácteo';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'zanahoria')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'zanahoria',g.Id,N'mejor tolerada cocida',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'verdura';
IF NOT EXISTS(SELECT 1 FROM PlatIngrediente WHERE Nombre=N'zapallito italiano')
  INSERT PlatIngrediente(Nombre,GrupoId,NotasEII,Activo,FechaCreacion)
  SELECT N'zapallito italiano',g.Id,N'',1,SYSUTCDATETIME() FROM PlatGrupo g WHERE g.Nombre=N'verdura';
GO

-- ============ 3) ATRIBUTOS INTRINSECOS ============
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'aceite' AND a.Nombre=N'graso')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'aceite' AND a.Nombre=N'graso';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'aceite de oliva' AND a.Nombre=N'graso')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'aceite de oliva' AND a.Nombre=N'graso';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'acelga' AND a.Nombre=N'fibra-insoluble')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'acelga' AND a.Nombre=N'fibra-insoluble';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'aguacate' AND a.Nombre=N'graso')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'aguacate' AND a.Nombre=N'graso';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'almendra' AND a.Nombre=N'graso')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'almendra' AND a.Nombre=N'graso';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'almendra' AND a.Nombre=N'fibra-insoluble')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'almendra' AND a.Nombre=N'fibra-insoluble';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'apio' AND a.Nombre=N'fibra-insoluble')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'apio' AND a.Nombre=N'fibra-insoluble';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'brioche' AND a.Nombre=N'gluten')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'brioche' AND a.Nombre=N'gluten';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'brioche' AND a.Nombre=N'lactosa')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'brioche' AND a.Nombre=N'lactosa';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'cebolla' AND a.Nombre=N'fibra-insoluble')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'cebolla' AND a.Nombre=N'fibra-insoluble';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'cebollín' AND a.Nombre=N'fibra-insoluble')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'cebollín' AND a.Nombre=N'fibra-insoluble';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'cerveza' AND a.Nombre=N'alcohol')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'cerveza' AND a.Nombre=N'alcohol';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'cerveza' AND a.Nombre=N'gluten')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'cerveza' AND a.Nombre=N'gluten';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'chiltepín' AND a.Nombre=N'picante')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'chiltepín' AND a.Nombre=N'picante';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'col' AND a.Nombre=N'fibra-insoluble')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'col' AND a.Nombre=N'fibra-insoluble';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'coliflor' AND a.Nombre=N'fibra-insoluble')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'coliflor' AND a.Nombre=N'fibra-insoluble';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'crema' AND a.Nombre=N'lactosa')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'crema' AND a.Nombre=N'lactosa';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'crema' AND a.Nombre=N'graso')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'crema' AND a.Nombre=N'graso';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'fresa' AND a.Nombre=N'fibra-insoluble')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'fresa' AND a.Nombre=N'fibra-insoluble';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'harina de trigo' AND a.Nombre=N'gluten')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'harina de trigo' AND a.Nombre=N'gluten';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'jalapeño' AND a.Nombre=N'picante')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'jalapeño' AND a.Nombre=N'picante';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'leche' AND a.Nombre=N'lactosa')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'leche' AND a.Nombre=N'lactosa';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'leche de coco' AND a.Nombre=N'graso')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'leche de coco' AND a.Nombre=N'graso';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'lechuga' AND a.Nombre=N'fibra-insoluble')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'lechuga' AND a.Nombre=N'fibra-insoluble';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'limón' AND a.Nombre=N'cítrico')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'limón' AND a.Nombre=N'cítrico';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'manzana' AND a.Nombre=N'fibra-insoluble')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'manzana' AND a.Nombre=N'fibra-insoluble';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'mayonesa' AND a.Nombre=N'graso')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'mayonesa' AND a.Nombre=N'graso';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'naranja' AND a.Nombre=N'cítrico')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'naranja' AND a.Nombre=N'cítrico';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'pan pita' AND a.Nombre=N'gluten')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'pan pita' AND a.Nombre=N'gluten';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'pepino' AND a.Nombre=N'fibra-insoluble')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'pepino' AND a.Nombre=N'fibra-insoluble';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'pera' AND a.Nombre=N'fibra-insoluble')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'pera' AND a.Nombre=N'fibra-insoluble';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'pimienta' AND a.Nombre=N'picante')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'pimienta' AND a.Nombre=N'picante';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'queso' AND a.Nombre=N'lactosa')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'queso' AND a.Nombre=N'lactosa';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'queso' AND a.Nombre=N'graso')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'queso' AND a.Nombre=N'graso';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'salsa de soya' AND a.Nombre=N'gluten')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'salsa de soya' AND a.Nombre=N'gluten';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'tocino' AND a.Nombre=N'graso')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'tocino' AND a.Nombre=N'graso';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'tostada de maíz' AND a.Nombre=N'graso')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'tostada de maíz' AND a.Nombre=N'graso';
IF NOT EXISTS(SELECT 1 FROM PlatIngredienteAtributo ia JOIN PlatIngrediente i ON i.Id=ia.IngredienteId JOIN PlatAtributo a ON a.Id=ia.AtributoId WHERE i.Nombre=N'yogur' AND a.Nombre=N'lactosa')
  INSERT PlatIngredienteAtributo(IngredienteId,AtributoId) SELECT i.Id,a.Id FROM PlatIngrediente i,PlatAtributo a WHERE i.Nombre=N'yogur' AND a.Nombre=N'lactosa';
GO

-- ============ 4) PLATILLOS ============
IF NOT EXISTS(SELECT 1 FROM PlatPlatillo WHERE Codigo=N'P001')
  INSERT PlatPlatillo(Codigo,Nombre,CategoriaId,Porciones,TiempoPrepMin,Dificultad,PasosResumidos,FuenteNombre,FuenteUrl,Notas,Activo,FechaCreacion)
  SELECT N'P001',N'Huevos en nido de brioche',c.Id,4,20,N'Fácil',N'Vaciar los brioches, poner beicon y un huevo dentro, rociar con crema y hornear ~10 min a 180°C.',N'Libro de recetas exprés',N'(revista impresa)',N'Tiene lácteos y gluten',1,SYSUTCDATETIME()
  FROM PlatCategoria c WHERE c.Nombre=N'Entrada';
IF NOT EXISTS(SELECT 1 FROM PlatPlatillo WHERE Codigo=N'P002')
  INSERT PlatPlatillo(Codigo,Nombre,CategoriaId,Porciones,TiempoPrepMin,Dificultad,PasosResumidos,FuenteNombre,FuenteUrl,Notas,Activo,FechaCreacion)
  SELECT N'P002',N'Tacos de pescado estilo Ensenada',c.Id,4,40,N'Media',N'Capear el pescado con masa de harina, huevo y cerveza; freír y servir en tortilla con aderezo de mayonesa y col.',N'Blog de cocina mexicana',N'https://ejemplo.com/tacos',N'Frito y con lácteos; pesado para brote',1,SYSUTCDATETIME()
  FROM PlatCategoria c WHERE c.Nombre=N'Plato fuerte';
IF NOT EXISTS(SELECT 1 FROM PlatPlatillo WHERE Codigo=N'P003')
  INSERT PlatPlatillo(Codigo,Nombre,CategoriaId,Porciones,TiempoPrepMin,Dificultad,PasosResumidos,FuenteNombre,FuenteUrl,Notas,Activo,FechaCreacion)
  SELECT N'P003',N'Tostadas de camarón seco con jalapeño',c.Id,8,10,N'Media',N'Mezclar camarón seco hidratado con verduras picadas y jugos cítricos; servir sobre tostada con mayonesa y aguacate.',N'Sitio de marca',N'https://ejemplo.com/tostadas',N'Picante y cítrico; camarón',1,SYSUTCDATETIME()
  FROM PlatCategoria c WHERE c.Nombre=N'Entrada';
IF NOT EXISTS(SELECT 1 FROM PlatPlatillo WHERE Codigo=N'P004')
  INSERT PlatPlatillo(Codigo,Nombre,CategoriaId,Porciones,TiempoPrepMin,Dificultad,PasosResumidos,FuenteNombre,FuenteUrl,Notas,Activo,FechaCreacion)
  SELECT N'P004',N'Aguachile estilo sinaloense',c.Id,4,20,N'Fácil',N'Licuar limón con salsa de soya y chiles; marinar el camarón crudo con pepino y cebolla 1 hora; servir con aguacate.',N'Sitio de marca',N'https://ejemplo.com/aguachile',N'Camarón CRUDO + muy picante; evitar en brote',1,SYSUTCDATETIME()
  FROM PlatCategoria c WHERE c.Nombre=N'Entrada';
IF NOT EXISTS(SELECT 1 FROM PlatPlatillo WHERE Codigo=N'P005')
  INSERT PlatPlatillo(Codigo,Nombre,CategoriaId,Porciones,TiempoPrepMin,Dificultad,PasosResumidos,FuenteNombre,FuenteUrl,Notas,Activo,FechaCreacion)
  SELECT N'P005',N'Pollo al cilantro con arroz al pimiento',c.Id,4,60,N'Difícil',N'Machaca el jengibre, la pimienta, el cilantro, la ralladura de limón y la cebolla hasta formar una pasta. Cocínala con caldo de pollo, leche de coco y hojas de limón; luego sírvela sobre el pollo previamente cocido y cortado en cubos.',N'Libro de cocina saludable',N'(revista impresa)',NULL,1,SYSUTCDATETIME()
  FROM PlatCategoria c WHERE c.Nombre=N'Plato fuerte';
IF NOT EXISTS(SELECT 1 FROM PlatPlatillo WHERE Codigo=N'P006')
  INSERT PlatPlatillo(Codigo,Nombre,CategoriaId,Porciones,TiempoPrepMin,Dificultad,PasosResumidos,FuenteNombre,FuenteUrl,Notas,Activo,FechaCreacion)
  SELECT N'P006',N'Ensalada de Betabel con Zanahoria',c.Id,6,20,N'Fácil',N'Pela y ralla las zanahorias y los betabeles, mézclalos en un recipiente y aliña con el aderezo de tu preferencia. Revuelve bien antes de servir.',N'Libro de cocina saludable',N'(revista impresa)',NULL,1,SYSUTCDATETIME()
  FROM PlatCategoria c WHERE c.Nombre=N'Ensalada';
IF NOT EXISTS(SELECT 1 FROM PlatPlatillo WHERE Codigo=N'P007')
  INSERT PlatPlatillo(Codigo,Nombre,CategoriaId,Porciones,TiempoPrepMin,Dificultad,PasosResumidos,FuenteNombre,FuenteUrl,Notas,Activo,FechaCreacion)
  SELECT N'P007',N'Arroz con Champiñones',c.Id,4,40,N'Media',N'Sofríe el arroz con tomate, champiñones y cebolla, añade agua y sazona al gusto. Cocina tapado durante 30 minutos, incorpora las arvejas, mezcla y sirve.',N'Libro de cocina saludable',N'(revista impresa)',NULL,1,SYSUTCDATETIME()
  FROM PlatCategoria c WHERE c.Nombre=N'Plato fuerte';
IF NOT EXISTS(SELECT 1 FROM PlatPlatillo WHERE Codigo=N'P008')
  INSERT PlatPlatillo(Codigo,Nombre,CategoriaId,Porciones,TiempoPrepMin,Dificultad,PasosResumidos,FuenteNombre,FuenteUrl,Notas,Activo,FechaCreacion)
  SELECT N'P008',N'Huevos Falsos',c.Id,4,10,N'Fácil',N'Sirve cinco cucharadas de yogur en un plato como base y coloca encima dos mitades de durazno en conserva. Presenta el postre de inmediato.',N'Libro de cocina saludable',N'(revista impresa)',N'Dulce y tiene lácteos',1,SYSUTCDATETIME()
  FROM PlatCategoria c WHERE c.Nombre=N'Postre';
IF NOT EXISTS(SELECT 1 FROM PlatPlatillo WHERE Codigo=N'P009')
  INSERT PlatPlatillo(Codigo,Nombre,CategoriaId,Porciones,TiempoPrepMin,Dificultad,PasosResumidos,FuenteNombre,FuenteUrl,Notas,Activo,FechaCreacion)
  SELECT N'P009',N'Quesadillas light',c.Id,2,20,N'Fácil',N'Cuece el huevo y córtalo en rodajas; mientras tanto, tritura el aguacate y rebana el tomate. Rellena las mitades del pan pita con el huevo, el tomate, el aguacate y la lechuga, luego córtalo en cuatro porciones y sirve.',N'Libro de cocina saludable',N'(revista impresa)',NULL,1,SYSUTCDATETIME()
  FROM PlatCategoria c WHERE c.Nombre=N'Snack';
IF NOT EXISTS(SELECT 1 FROM PlatPlatillo WHERE Codigo=N'P010')
  INSERT PlatPlatillo(Codigo,Nombre,CategoriaId,Porciones,TiempoPrepMin,Dificultad,PasosResumidos,FuenteNombre,FuenteUrl,Notas,Activo,FechaCreacion)
  SELECT N'P010',N'Salpicón con atún',c.Id,6,30,N'Fácil',N'Corta la lechuga en juliana y cocina las papas, la zanahoria y el huevo; luego pica las verduras en cubos y el huevo en tiras. Mezcla con el atún desmenuzado, aliña al gusto y sirve.',N'Libro de cocina saludable',N'(revista impresa)',NULL,1,SYSUTCDATETIME()
  FROM PlatCategoria c WHERE c.Nombre=N'Ensalada';
IF NOT EXISTS(SELECT 1 FROM PlatPlatillo WHERE Codigo=N'P011')
  INSERT PlatPlatillo(Codigo,Nombre,CategoriaId,Porciones,TiempoPrepMin,Dificultad,PasosResumidos,FuenteNombre,FuenteUrl,Notas,Activo,FechaCreacion)
  SELECT N'P011',N'Lasaña de acelga y queso',c.Id,6,60,N'Difícil',N'Saltea las verduras picadas, blanquea las hojas de acelga y reserva. Arma la lasaña alternando capas de acelga, verduras y queso, finalizando con una capa de queso antes de hornear o servir.',N'Recetario de nutricion',N'(revista impresa)',NULL,1,SYSUTCDATETIME()
  FROM PlatCategoria c WHERE c.Nombre=N'Plato fuerte';
IF NOT EXISTS(SELECT 1 FROM PlatPlatillo WHERE Codigo=N'P012')
  INSERT PlatPlatillo(Codigo,Nombre,CategoriaId,Porciones,TiempoPrepMin,Dificultad,PasosResumidos,FuenteNombre,FuenteUrl,Notas,Activo,FechaCreacion)
  SELECT N'P012',N'Pollo a la Naranja',c.Id,4,50,N'Difícil',N'Condimenta el pollo y hornéalo con hojas de laurel, jugo de naranja y aceite, bañándolo ocasionalmente con su jugo de cocción. Deja enfriar, córtalo en presas y sirve.',N'Recetario de nutricion',N'(revista impresa)',NULL,1,SYSUTCDATETIME()
  FROM PlatCategoria c WHERE c.Nombre=N'Plato fuerte';
IF NOT EXISTS(SELECT 1 FROM PlatPlatillo WHERE Codigo=N'P013')
  INSERT PlatPlatillo(Codigo,Nombre,CategoriaId,Porciones,TiempoPrepMin,Dificultad,PasosResumidos,FuenteNombre,FuenteUrl,Notas,Activo,FechaCreacion)
  SELECT N'P013',N'Ensalada de fruta',c.Id,4,20,N'Fácil',N'Pelar una naranja y dos kiwis, retirar las pepas a una 
manzana y una pera, cortar en rebanadas y servir. 
Mezclar con jugo de naranja recién exprimido y 
servir.',N'Recetario de nutricion',N'(revista impresa)',NULL,1,SYSUTCDATETIME()
  FROM PlatCategoria c WHERE c.Nombre=N'Ensalada';
IF NOT EXISTS(SELECT 1 FROM PlatPlatillo WHERE Codigo=N'P014')
  INSERT PlatPlatillo(Codigo,Nombre,CategoriaId,Porciones,TiempoPrepMin,Dificultad,PasosResumidos,FuenteNombre,FuenteUrl,Notas,Activo,FechaCreacion)
  SELECT N'P014',N'Tuti fruti y yogurt natural descremado',c.Id,4,30,N'Fácil',N'Mezcla el melón, las frutillas y las cerezas en un recipiente, luego incorpora el yogur natural y revuelve suavemente. Sirve de inmediato como una ensalada de frutas fresca.',N'Recetario de nutricion',N'(revista impresa)',NULL,1,SYSUTCDATETIME()
  FROM PlatCategoria c WHERE c.Nombre=N'Postre';
IF NOT EXISTS(SELECT 1 FROM PlatPlatillo WHERE Codigo=N'P015')
  INSERT PlatPlatillo(Codigo,Nombre,CategoriaId,Porciones,TiempoPrepMin,Dificultad,PasosResumidos,FuenteNombre,FuenteUrl,Notas,Activo,FechaCreacion)
  SELECT N'P015',N'Crema de coliflor',c.Id,6,40,N'Media',N'Tuesta ligeramente las almendras y cocina la cebolla con la coliflor y el caldo hasta que estén tiernas. Licúa la preparación con las almendras, sazona con sal y pimienta, y sirve caliente.',N'Recetario de nutricion',N'(revista impresa)',NULL,1,SYSUTCDATETIME()
  FROM PlatCategoria c WHERE c.Nombre=N'Sopa';
IF NOT EXISTS(SELECT 1 FROM PlatPlatillo WHERE Codigo=N'P016')
  INSERT PlatPlatillo(Codigo,Nombre,CategoriaId,Porciones,TiempoPrepMin,Dificultad,PasosResumidos,FuenteNombre,FuenteUrl,Notas,Activo,FechaCreacion)
  SELECT N'P016',N'Ensalada Coob',c.Id,6,30,N'Fácil',N'Licúa el vinagre, la leche, el yogur y la sal para preparar el aliño. Arma la torre en capas con lechuga, tomate, palta, choclo y pollo, desmolda, decora con huevo espolvoreado y un poco de aliño.',N'Recetario de nutricion',N'(revista impresa)',NULL,1,SYSUTCDATETIME()
  FROM PlatCategoria c WHERE c.Nombre=N'Ensalada';
IF NOT EXISTS(SELECT 1 FROM PlatPlatillo WHERE Codigo=N'P017')
  INSERT PlatPlatillo(Codigo,Nombre,CategoriaId,Porciones,TiempoPrepMin,Dificultad,PasosResumidos,FuenteNombre,FuenteUrl,Notas,Activo,FechaCreacion)
  SELECT N'P017',N'tortillas de atun',c.Id,2,20,N'Fácil',N'Bate las claras, añade las yemas, sal y el atún previamente escurrido. Cocina la mezcla en un sartén con aceite por ambos lados hasta que la tortilla esté firme y dorada.',N'Recetario de nutricion',N'Recetario de nutricion',NULL,1,SYSUTCDATETIME()
  FROM PlatCategoria c WHERE c.Nombre=N'Entrada';
GO

-- ============ 5) RELACION PLATILLO-INGREDIENTE ============
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'4 huevos',4,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P001' AND i.Nombre=N'huevo'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'4 huevos');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'4 brioches pequeños',4,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P001' AND i.Nombre=N'brioche'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'4 brioches pequeños');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'200 g de cintas de beicon',200,(SELECT Id FROM PlatUnidad WHERE Nombre=N'g'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P001' AND i.Nombre=N'tocino'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'200 g de cintas de beicon');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'4 cucharaditas de nata líquida 15%',4,(SELECT Id FROM PlatUnidad WHERE Nombre=N'cdta'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P001' AND i.Nombre=N'crema'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'4 cucharaditas de nata líquida 15%');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'Sal y pimienta',NULL,(SELECT Id FROM PlatUnidad WHERE Nombre=N'al gusto'),1,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P001' AND i.Nombre=N'sal'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'Sal y pimienta');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1 kg de filetes de pescado',1,(SELECT Id FROM PlatUnidad WHERE Nombre=N'kg'),0,N'en tiras'
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P002' AND i.Nombre=N'pescado blanco'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1 kg de filetes de pescado');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1/2 taza de harina',0.5,(SELECT Id FROM PlatUnidad WHERE Nombre=N'taza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P002' AND i.Nombre=N'harina de trigo'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1/2 taza de harina');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'2 huevos enteros',2,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P002' AND i.Nombre=N'huevo'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'2 huevos enteros');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'2 tazas de cerveza',2,(SELECT Id FROM PlatUnidad WHERE Nombre=N'taza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P002' AND i.Nombre=N'cerveza'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'2 tazas de cerveza');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'Tortillas de maíz',NULL,(SELECT Id FROM PlatUnidad WHERE Nombre=N'al gusto'),1,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P002' AND i.Nombre=N'tortilla de maíz'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'Tortillas de maíz');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1/2 taza de mayonesa',0.5,(SELECT Id FROM PlatUnidad WHERE Nombre=N'taza'),0,N'aderezo'
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P002' AND i.Nombre=N'mayonesa'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1/2 taza de mayonesa');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1/4 taza de crema',0.25,(SELECT Id FROM PlatUnidad WHERE Nombre=N'taza'),0,N'aderezo'
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P002' AND i.Nombre=N'crema'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1/4 taza de crema');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1/4 taza de leche',0.25,(SELECT Id FROM PlatUnidad WHERE Nombre=N'taza'),0,N'aderezo'
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P002' AND i.Nombre=N'leche'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1/4 taza de leche');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'Col picada',NULL,(SELECT Id FROM PlatUnidad WHERE Nombre=N'al gusto'),1,N'cruda'
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P002' AND i.Nombre=N'col'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'Col picada');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'250 g camarón seco',250,(SELECT Id FROM PlatUnidad WHERE Nombre=N'g'),0,N'hidratado y triturado'
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P003' AND i.Nombre=N'camarón'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'250 g camarón seco');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'300 g zanahoria rallada',300,(SELECT Id FROM PlatUnidad WHERE Nombre=N'g'),0,N'rallada'
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P003' AND i.Nombre=N'zanahoria'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'300 g zanahoria rallada');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'135 g pepino en cubitos',135,(SELECT Id FROM PlatUnidad WHERE Nombre=N'g'),0,N'crudo'
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P003' AND i.Nombre=N'pepino'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'135 g pepino en cubitos');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'100 g cebolla picada',100,(SELECT Id FROM PlatUnidad WHERE Nombre=N'g'),0,N'cruda'
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P003' AND i.Nombre=N'cebolla'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'100 g cebolla picada');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'15 g cilantro picado',15,(SELECT Id FROM PlatUnidad WHERE Nombre=N'g'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P003' AND i.Nombre=N'cilantro'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'15 g cilantro picado');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'50 g nachos de jalapeño La Costeña',50,(SELECT Id FROM PlatUnidad WHERE Nombre=N'g'),0,N'en escabeche'
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P003' AND i.Nombre=N'jalapeño'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'50 g nachos de jalapeño La Costeña');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'160 ml jugo de limón',160,(SELECT Id FROM PlatUnidad WHERE Nombre=N'ml'),0,N'jugo'
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P003' AND i.Nombre=N'limón'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'160 ml jugo de limón');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'80 ml jugo de naranja',80,(SELECT Id FROM PlatUnidad WHERE Nombre=N'ml'),0,N'jugo'
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P003' AND i.Nombre=N'naranja'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'80 ml jugo de naranja');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'16 tostadas de maíz',16,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P003' AND i.Nombre=N'tostada de maíz'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'16 tostadas de maíz');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'240 ml mayonesa con limón La Costeña',240,(SELECT Id FROM PlatUnidad WHERE Nombre=N'ml'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P003' AND i.Nombre=N'mayonesa'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'240 ml mayonesa con limón La Costeña');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'150 g aguacate rebanado',150,(SELECT Id FROM PlatUnidad WHERE Nombre=N'g'),0,N'rebanado'
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P003' AND i.Nombre=N'aguacate'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'150 g aguacate rebanado');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'3/4 taza de jugo de limón',0.75,(SELECT Id FROM PlatUnidad WHERE Nombre=N'taza'),0,N'colado'
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P004' AND i.Nombre=N'limón'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'3/4 taza de jugo de limón');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1/2 taza de salsa de soya Maggi',0.5,(SELECT Id FROM PlatUnidad WHERE Nombre=N'taza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P004' AND i.Nombre=N'salsa de soya'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1/2 taza de salsa de soya Maggi');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'3 cucharadas de chile chiltepín',3,(SELECT Id FROM PlatUnidad WHERE Nombre=N'cda'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P004' AND i.Nombre=N'chiltepín'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'3 cucharadas de chile chiltepín');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1 kg de camarón limpio',1,(SELECT Id FROM PlatUnidad WHERE Nombre=N'kg'),0,N'crudo, en mariposa'
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P004' AND i.Nombre=N'camarón'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1 kg de camarón limpio');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1½ pepinos sin semillas',1.5,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N'crudo, en medias lunas'
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P004' AND i.Nombre=N'pepino'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1½ pepinos sin semillas');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1/2 cebolla morada fileteada',0.5,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N'cruda'
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P004' AND i.Nombre=N'cebolla'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1/2 cebolla morada fileteada');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'2 aguacates rebanados',2,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N'rebanado'
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P004' AND i.Nombre=N'aguacate'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'2 aguacates rebanados');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1 paquete tostadas de maíz azul',1,(SELECT Id FROM PlatUnidad WHERE Nombre=N'paquete'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P004' AND i.Nombre=N'tostada de maíz'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1 paquete tostadas de maíz azul');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'½ kilo de filetes de pollo',0.5,(SELECT Id FROM PlatUnidad WHERE Nombre=N'kg'),0,N'en tiras'
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'p005' AND i.Nombre=N'pollo'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'½ kilo de filetes de pollo');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1 cucharadita de granos de 
 pimienta negra',1,(SELECT Id FROM PlatUnidad WHERE Nombre=N'cdta'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'p005' AND i.Nombre=N'pimienta'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1 cucharadita de granos de 
 pimienta negra');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'2 cucharaditas de jengibre en polvo',2,(SELECT Id FROM PlatUnidad WHERE Nombre=N'cdta'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'p005' AND i.Nombre=N'jengibre'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'2 cucharaditas de jengibre en polvo');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1 atado de cilantro picado fino',1,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'p005' AND i.Nombre=N'cilantro'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1 atado de cilantro picado fino');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'½ cebolla picada fina en cuadritos',0.5,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N'cruda'
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'p005' AND i.Nombre=N'cebolla'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'½ cebolla picada fina en cuadritos');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'ralladura de 1 limón',1,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N'Rallada'
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'p005' AND i.Nombre=N'limón'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'ralladura de 1 limón');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'400 ml de leche de coco',400,(SELECT Id FROM PlatUnidad WHERE Nombre=N'ml'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'p005' AND i.Nombre=N'leche de coco'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'400 ml de leche de coco');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'4 hojas de limón',4,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'p005' AND i.Nombre=N'limón'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'4 hojas de limón');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1 manojo de albahaca fresca (opcional)',1,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'p005' AND i.Nombre=N'albahaca'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1 manojo de albahaca fresca (opcional)');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'Sal',NULL,(SELECT Id FROM PlatUnidad WHERE Nombre=N'al gusto'),1,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'p005' AND i.Nombre=N'sal'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'Sal');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'3 betabeles',3,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'p006' AND i.Nombre=N'betabel'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'3 betabeles');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'3 Zanahorias',3,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'p006' AND i.Nombre=N'zanahoria'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'3 Zanahorias');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'Sal',NULL,(SELECT Id FROM PlatUnidad WHERE Nombre=N'al gusto'),1,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'p006' AND i.Nombre=N'sal'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'Sal');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'Aceite de oliva',NULL,(SELECT Id FROM PlatUnidad WHERE Nombre=N'al gusto'),1,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'p006' AND i.Nombre=N'aceite de oliva'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'Aceite de oliva');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'limón',NULL,(SELECT Id FROM PlatUnidad WHERE Nombre=N'al gusto'),1,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'p006' AND i.Nombre=N'limón'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'limón');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1 taza de arroz',1,(SELECT Id FROM PlatUnidad WHERE Nombre=N'taza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P007' AND i.Nombre=N'arroz'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1 taza de arroz');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1 bandeja de champiñones cortados finitos ',1,(SELECT Id FROM PlatUnidad WHERE Nombre=N'bandeja'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P007' AND i.Nombre=N'champiñón'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1 bandeja de champiñones cortados finitos ');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1 taza de arvejas cocidas',1,(SELECT Id FROM PlatUnidad WHERE Nombre=N'taza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P007' AND i.Nombre=N'arveja'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1 taza de arvejas cocidas');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'3 tazas de agua',3,(SELECT Id FROM PlatUnidad WHERE Nombre=N'taza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P007' AND i.Nombre=N'agua'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'3 tazas de agua');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'½ taza de cebolla picada fina ',0.5,(SELECT Id FROM PlatUnidad WHERE Nombre=N'taza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P007' AND i.Nombre=N'cebolla'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'½ taza de cebolla picada fina ');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1 taza de tomate picado',1,(SELECT Id FROM PlatUnidad WHERE Nombre=N'taza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P007' AND i.Nombre=N'tomate'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1 taza de tomate picado');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'2 cucharadas de aceite ',2,(SELECT Id FROM PlatUnidad WHERE Nombre=N'cda'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P007' AND i.Nombre=N'aceite'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'2 cucharadas de aceite ');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'sal y pimienta a gusto ',NULL,(SELECT Id FROM PlatUnidad WHERE Nombre=N'al gusto'),1,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P007' AND i.Nombre=N'sal'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'sal y pimienta a gusto ');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1 tarro de duraznos en conserva',1,(SELECT Id FROM PlatUnidad WHERE Nombre=N'tarro'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P008' AND i.Nombre=N'durazno'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1 tarro de duraznos en conserva');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'2 yogurt descremados natural o vainilla',2,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P008' AND i.Nombre=N'yogur'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'2 yogurt descremados natural o vainilla');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1 Pan pita',1,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P009' AND i.Nombre=N'pan pita'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1 Pan pita');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1/2 huevo',0.5,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P009' AND i.Nombre=N'huevo'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1/2 huevo');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1/2 tomate ',0.5,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P009' AND i.Nombre=N'tomate'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1/2 tomate ');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1/4 taza de lechuga',0.25,(SELECT Id FROM PlatUnidad WHERE Nombre=N'taza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P009' AND i.Nombre=N'lechuga'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1/4 taza de lechuga');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1/2 aguacate',0.5,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P009' AND i.Nombre=N'aguacate'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1/2 aguacate');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'2 lechugas',2,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P010' AND i.Nombre=N'lechuga'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'2 lechugas');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1 zanahoria',1,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P010' AND i.Nombre=N'zanahoria'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1 zanahoria');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'2 tarros de atún al agua',2,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P010' AND i.Nombre=N'atún'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'2 tarros de atún al agua');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1 huevo',1,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P010' AND i.Nombre=N'huevo'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1 huevo');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'3 papas ',3,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P010' AND i.Nombre=N'papa'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'3 papas ');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1 paquete de acelgas',1,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P011' AND i.Nombre=N'acelga'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1 paquete de acelgas');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'2 tomates',2,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P011' AND i.Nombre=N'tomate'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'2 tomates');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1 zanahoria',1,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P011' AND i.Nombre=N'zanahoria'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1 zanahoria');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1/2 cebolla',0.5,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P011' AND i.Nombre=N'cebolla'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1/2 cebolla');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'4 ramas de apio',4,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P011' AND i.Nombre=N'apio'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'4 ramas de apio');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'8 champiñones',8,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P011' AND i.Nombre=N'champiñón'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'8 champiñones');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1/2 zapallito italiano',0.5,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P011' AND i.Nombre=N'zapallito italiano'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1/2 zapallito italiano');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'8 laminas de queso',8,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P011' AND i.Nombre=N'queso'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'8 laminas de queso');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'20 ml de aceite',20,(SELECT Id FROM PlatUnidad WHERE Nombre=N'ml'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P011' AND i.Nombre=N'aceite'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'20 ml de aceite');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1 pollo mediano',1,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P012' AND i.Nombre=N'pollo'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1 pollo mediano');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'6 hojas de laurel',6,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P012' AND i.Nombre=N'laurel'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'6 hojas de laurel');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'6 naranjas',6,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P012' AND i.Nombre=N'naranja'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'6 naranjas');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'3 cucharadas de aceite (de preferencia de oliva)',3,(SELECT Id FROM PlatUnidad WHERE Nombre=N'cda'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P012' AND i.Nombre=N'aceite de oliva'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'3 cucharadas de aceite (de preferencia de oliva)');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'sal, pimienta',NULL,(SELECT Id FROM PlatUnidad WHERE Nombre=N'al gusto'),1,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P012' AND i.Nombre=N'sal'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'sal, pimienta');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1 naranja',1,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P013' AND i.Nombre=N'naranja'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1 naranja');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'2 kiwis',2,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P013' AND i.Nombre=N'kiwi'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'2 kiwis');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1 pera',1,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P013' AND i.Nombre=N'pera'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1 pera');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1 manzana',1,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P013' AND i.Nombre=N'manzana'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1 manzana');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1 taza de jugo de naranja',1,(SELECT Id FROM PlatUnidad WHERE Nombre=N'taza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P013' AND i.Nombre=N'naranja'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1 taza de jugo de naranja');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1 taza de melón',1,(SELECT Id FROM PlatUnidad WHERE Nombre=N'taza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P014' AND i.Nombre=N'melón'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1 taza de melón');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1 taza de frutillas',1,(SELECT Id FROM PlatUnidad WHERE Nombre=N'taza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P014' AND i.Nombre=N'fresa'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1 taza de frutillas');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'15 cerezas',15,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P014' AND i.Nombre=N'cerveza'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'15 cerezas');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1 yogurt',1,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P014' AND i.Nombre=N'yogur'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1 yogurt');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'750 g de coliflor',750,(SELECT Id FROM PlatUnidad WHERE Nombre=N'g'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P015' AND i.Nombre=N'coliflor'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'750 g de coliflor');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'1 cebolla picada',1,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P015' AND i.Nombre=N'cebolla'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'1 cebolla picada');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'200 g de almendras',200,(SELECT Id FROM PlatUnidad WHERE Nombre=N'g'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P015' AND i.Nombre=N'almendra'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'200 g de almendras');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'800 ml de caldo de ave',800,(SELECT Id FROM PlatUnidad WHERE Nombre=N'ml'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P015' AND i.Nombre=N'caldo de pollo'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'800 ml de caldo de ave');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'3 cucharadas de aceite',3,(SELECT Id FROM PlatUnidad WHERE Nombre=N'cda'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P015' AND i.Nombre=N'aceite'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'3 cucharadas de aceite');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'sal ',NULL,(SELECT Id FROM PlatUnidad WHERE Nombre=N'al gusto'),1,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P015' AND i.Nombre=N'sal'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'sal ');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'2 cebollines',2,(SELECT Id FROM PlatUnidad WHERE Nombre=N'pieza'),0,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P015' AND i.Nombre=N'cebollín'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'2 cebollines');
INSERT PlatPlatilloIngrediente(PlatilloId,IngredienteId,TextoOriginal,Cantidad,UnidadId,EsAlGusto,NotaPreparacion)
  SELECT p.Id,i.Id,N'pimienta',NULL,(SELECT Id FROM PlatUnidad WHERE Nombre=N'al gusto'),1,N''
  FROM PlatPlatillo p, PlatIngrediente i WHERE p.Codigo=N'P015' AND i.Nombre=N'pimienta'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngrediente x WHERE x.PlatilloId=p.Id AND x.IngredienteId=i.Id AND ISNULL(x.TextoOriginal,'')=N'pimienta');
GO

-- ============ 6) ATRIBUTOS DE USO (crudo/frito/en jugo) ============
INSERT PlatPlatilloIngredienteAtributo(PlatilloIngredienteId,AtributoId)
  SELECT pi.Id,a.Id FROM PlatPlatilloIngrediente pi
    JOIN PlatPlatillo p ON p.Id=pi.PlatilloId JOIN PlatIngrediente i ON i.Id=pi.IngredienteId, PlatAtributo a
  WHERE p.Codigo=N'P002' AND i.Nombre=N'col' AND ISNULL(pi.TextoOriginal,'')=N'Col picada' AND a.Nombre=N'crudo'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngredienteAtributo x WHERE x.PlatilloIngredienteId=pi.Id AND x.AtributoId=a.Id);
INSERT PlatPlatilloIngredienteAtributo(PlatilloIngredienteId,AtributoId)
  SELECT pi.Id,a.Id FROM PlatPlatilloIngrediente pi
    JOIN PlatPlatillo p ON p.Id=pi.PlatilloId JOIN PlatIngrediente i ON i.Id=pi.IngredienteId, PlatAtributo a
  WHERE p.Codigo=N'P003' AND i.Nombre=N'pepino' AND ISNULL(pi.TextoOriginal,'')=N'135 g pepino en cubitos' AND a.Nombre=N'crudo'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngredienteAtributo x WHERE x.PlatilloIngredienteId=pi.Id AND x.AtributoId=a.Id);
INSERT PlatPlatilloIngredienteAtributo(PlatilloIngredienteId,AtributoId)
  SELECT pi.Id,a.Id FROM PlatPlatilloIngrediente pi
    JOIN PlatPlatillo p ON p.Id=pi.PlatilloId JOIN PlatIngrediente i ON i.Id=pi.IngredienteId, PlatAtributo a
  WHERE p.Codigo=N'P003' AND i.Nombre=N'cebolla' AND ISNULL(pi.TextoOriginal,'')=N'100 g cebolla picada' AND a.Nombre=N'crudo'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngredienteAtributo x WHERE x.PlatilloIngredienteId=pi.Id AND x.AtributoId=a.Id);
INSERT PlatPlatilloIngredienteAtributo(PlatilloIngredienteId,AtributoId)
  SELECT pi.Id,a.Id FROM PlatPlatilloIngrediente pi
    JOIN PlatPlatillo p ON p.Id=pi.PlatilloId JOIN PlatIngrediente i ON i.Id=pi.IngredienteId, PlatAtributo a
  WHERE p.Codigo=N'P003' AND i.Nombre=N'limón' AND ISNULL(pi.TextoOriginal,'')=N'160 ml jugo de limón' AND a.Nombre=N'en jugo'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngredienteAtributo x WHERE x.PlatilloIngredienteId=pi.Id AND x.AtributoId=a.Id);
INSERT PlatPlatilloIngredienteAtributo(PlatilloIngredienteId,AtributoId)
  SELECT pi.Id,a.Id FROM PlatPlatilloIngrediente pi
    JOIN PlatPlatillo p ON p.Id=pi.PlatilloId JOIN PlatIngrediente i ON i.Id=pi.IngredienteId, PlatAtributo a
  WHERE p.Codigo=N'P003' AND i.Nombre=N'naranja' AND ISNULL(pi.TextoOriginal,'')=N'80 ml jugo de naranja' AND a.Nombre=N'en jugo'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngredienteAtributo x WHERE x.PlatilloIngredienteId=pi.Id AND x.AtributoId=a.Id);
INSERT PlatPlatilloIngredienteAtributo(PlatilloIngredienteId,AtributoId)
  SELECT pi.Id,a.Id FROM PlatPlatilloIngrediente pi
    JOIN PlatPlatillo p ON p.Id=pi.PlatilloId JOIN PlatIngrediente i ON i.Id=pi.IngredienteId, PlatAtributo a
  WHERE p.Codigo=N'P004' AND i.Nombre=N'camarón' AND ISNULL(pi.TextoOriginal,'')=N'1 kg de camarón limpio' AND a.Nombre=N'crudo'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngredienteAtributo x WHERE x.PlatilloIngredienteId=pi.Id AND x.AtributoId=a.Id);
INSERT PlatPlatilloIngredienteAtributo(PlatilloIngredienteId,AtributoId)
  SELECT pi.Id,a.Id FROM PlatPlatilloIngrediente pi
    JOIN PlatPlatillo p ON p.Id=pi.PlatilloId JOIN PlatIngrediente i ON i.Id=pi.IngredienteId, PlatAtributo a
  WHERE p.Codigo=N'P004' AND i.Nombre=N'pepino' AND ISNULL(pi.TextoOriginal,'')=N'1½ pepinos sin semillas' AND a.Nombre=N'crudo'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngredienteAtributo x WHERE x.PlatilloIngredienteId=pi.Id AND x.AtributoId=a.Id);
INSERT PlatPlatilloIngredienteAtributo(PlatilloIngredienteId,AtributoId)
  SELECT pi.Id,a.Id FROM PlatPlatilloIngrediente pi
    JOIN PlatPlatillo p ON p.Id=pi.PlatilloId JOIN PlatIngrediente i ON i.Id=pi.IngredienteId, PlatAtributo a
  WHERE p.Codigo=N'P004' AND i.Nombre=N'cebolla' AND ISNULL(pi.TextoOriginal,'')=N'1/2 cebolla morada fileteada' AND a.Nombre=N'crudo'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngredienteAtributo x WHERE x.PlatilloIngredienteId=pi.Id AND x.AtributoId=a.Id);
INSERT PlatPlatilloIngredienteAtributo(PlatilloIngredienteId,AtributoId)
  SELECT pi.Id,a.Id FROM PlatPlatilloIngrediente pi
    JOIN PlatPlatillo p ON p.Id=pi.PlatilloId JOIN PlatIngrediente i ON i.Id=pi.IngredienteId, PlatAtributo a
  WHERE p.Codigo=N'p005' AND i.Nombre=N'cebolla' AND ISNULL(pi.TextoOriginal,'')=N'½ cebolla picada fina en cuadritos' AND a.Nombre=N'crudo'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngredienteAtributo x WHERE x.PlatilloIngredienteId=pi.Id AND x.AtributoId=a.Id);
INSERT PlatPlatilloIngredienteAtributo(PlatilloIngredienteId,AtributoId)
  SELECT pi.Id,a.Id FROM PlatPlatilloIngrediente pi
    JOIN PlatPlatillo p ON p.Id=pi.PlatilloId JOIN PlatIngrediente i ON i.Id=pi.IngredienteId, PlatAtributo a
  WHERE p.Codigo=N'P013' AND i.Nombre=N'naranja' AND ISNULL(pi.TextoOriginal,'')=N'1 taza de jugo de naranja' AND a.Nombre=N'en jugo'
    AND NOT EXISTS(SELECT 1 FROM PlatPlatilloIngredienteAtributo x WHERE x.PlatilloIngredienteId=pi.Id AND x.AtributoId=a.Id);
GO

-- ============ VERIFICACION ============
SELECT 'Grupos' AS Tabla, COUNT(*) AS Filas FROM PlatGrupo
UNION ALL SELECT 'Atributos', COUNT(*) FROM PlatAtributo
UNION ALL SELECT 'Ingredientes', COUNT(*) FROM PlatIngrediente
UNION ALL SELECT 'Platillos', COUNT(*) FROM PlatPlatillo
UNION ALL SELECT 'Platillo-Ingrediente', COUNT(*) FROM PlatPlatilloIngrediente
UNION ALL SELECT 'Atributos de uso', COUNT(*) FROM PlatPlatilloIngredienteAtributo;
-- Esperado: Grupos 18 | Atributos 11 | Ingredientes 57 | Platillos 17 | Relaciones 99 | Usos 10
GO