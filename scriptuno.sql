create database unobd;

create table `unobd`.`carta` (
	`id_carta` int not null auto_increment,
    `tipo_carta`varchar(9) not null,
    `color_carta` varchar(8) not null,
    `efecto` varchar(45) not null,
    `num_carta` int not null,
    primary key (`id_carta`));
    
create table `unobd`.`jugador` (
	`id_jugador` int not null auto_increment,
    `nombre_jug` varchar(30) null default null,
    `cant_cartas` int not null,
    `estado` varchar(10) not null,
    primary key (`id_jugador`));

CREATE TABLE `jugada` (
	`id_jugada` int NOT NULL AUTO_INCREMENT,
    `id_jugador` int NULL default null,
    `id_carta` int NOT NULL,
    `id_partida` int NOT NULL,
    PRIMARY KEY (`id_jugada`),
    KEY `id_jugador` (`id_jugador`),
    KEY `id_carta` (`id_carta`),
	CONSTRAINT `id_carta` 
		FOREIGN KEY (`id_carta`) 
        REFERENCES `carta` (`id_carta`),
	CONSTRAINT `id_jugador` 
		FOREIGN KEY (`id_jugador`) 
        REFERENCES `jugador` (`id_jugador`));

alter table carta
modify tipo_carta enum('ordinaria', 'especial') not null;

alter table carta
modify color_carta enum('rojo', 'amarillo', 'azul', 'verde'),
modify efecto enum('cambiar_color', 'cambiar_direccion', 'bloquear', '+2', '+4');

alter table carta
add constraint chk_num_carta check (num_carta <= 9 and num_carta >= 0);

insert into carta(tipo_carta, color_carta, efecto, num_carta) values
	('ordinaria', 'rojo', null, 1),
    ("ordinaria", "rojo", null, 2),
    ("ordinaria", "rojo", null, 3),
    ("ordinaria", "rojo", null, 4),
    ("ordinaria", "rojo", null, 5),
    ("ordinaria", "rojo", null, 6),
    ("ordinaria", "rojo", null, 7),
    ("ordinaria", "rojo", null, 8),
    ("ordinaria", "rojo", null, 9),
    ("ordinaria", "amarillo", null, 1),
    ("ordinaria", "amarillo", null, 2),
    ("ordinaria", "amarillo", null, 3),
    ("ordinaria", "amarillo", null, 4),
    ("ordinaria", "amarillo", null, 5),
    ("ordinaria", "amarillo", null, 6),
    ("ordinaria", "amarillo", null, 7),
    ("ordinaria", "amarillo", null, 8),
    ("ordinaria", "amarillo", null, 9),
    ("ordinaria", "azul", null, 1),
    ("ordinaria", "azul", null, 2),
    ("ordinaria", "azul", null, 3),
    ("ordinaria", "azul", null, 4),
    ("ordinaria", "azul", null, 5),
    ("ordinaria", "azul", null, 6),
    ("ordinaria", "azul", null, 7),
    ("ordinaria", "azul", null, 8),
    ("ordinaria", "azul", null, 9),
    ("ordinaria", "verde", null, 1),
    ("ordinaria", "verde", null, 2),
    ("ordinaria", "verde", null, 3),
    ("ordinaria", "verde", null, 4),
    ("ordinaria", "verde", null, 5),
    ("ordinaria", "verde", null, 6),
    ("ordinaria", "verde", null, 7),
    ("ordinaria", "verde", null, 8),
    ("ordinaria", "verde", null, 9);
    
select * from carta;

insert into carta(tipo_carta, color_carta, efecto, num_carta) values
	('especial', null, 'cambiar_color', null),
    ('especial', null, 'cambiar_color', null),
    ('especial', null, '+4', null),
    ('especial', null, '+4', null),
    ('especial', 'rojo', 'bloquear', null),
    ('especial', 'rojo', 'bloquear', null),
    ('especial', 'amarillo', 'bloquear', null),
    ('especial', 'amarillo', 'bloquear', null),
    ('especial', 'azul', 'bloquear', null),
    ('especial', 'azul', 'bloquear', null),
    ('especial', 'verde', 'bloquear', null),
    ('especial', 'verde', 'bloquear', null),
    ('especial', 'rojo', '+2', null),
    ('especial', 'rojo', '+2', null),
    ('especial', 'amarillo', '+2', null),
    ('especial', 'amarillo', '+2', null),
    ('especial', 'azul', '+2', null),
    ('especial', 'azul', '+2', null),
    ('especial', 'verde', '+2', null),
    ('especial', 'verde', '+2', null),
    ('especial', 'rojo', 'cambiar_direccion', null),
    ('especial', 'amarillo', 'cambiar_direccion', null),
    ('especial', 'azul', 'cambiar_direccion', null),
    ('especial', 'verde', 'cambiar_direccion', null);
    
    create table `unobd`.`partida` (
		`id_partida`int not null auto_increment,
        `fecha_hora` timestamp not null,
        `id_ganador` int null default null,
        primary key (`id_partida`));
        
	alter table jugada
    add id_partida int not null;
    
    alter table jugada
    add constraint `fk_jugada_partida`
		foreign key (`id_partida`)
        references `partida`(`id_partida`);