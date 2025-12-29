INSERT INTO public.line_features_1 (geom)
VALUES (
  ST_SetSRID(
    ST_MakeLine(
      ST_Point(0, 0),
      ST_Point(0, 100)
    ),
    26918
  )
);
